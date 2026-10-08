using Common.Application.Handlers;
using Common.Domain.Primitives;
using Common.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.UnitTests.Infrastructure;

/// <summary>
///     A bounded context in miniature - a shop with orders and invoices - to test the dispatch of
///     domain events without depending on a real one. It is wired the way
///     <c>AddBoundedContextDbContext</c> wires a real one: the <c>DbContext</c> belongs to a scope,
///     the interceptor resolves the handlers from that scope, and so they share the context. Only
///     the database is in-memory SQLite instead of PostgreSQL.
/// </summary>
internal sealed class ShopFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AsyncServiceScope _scope;
    private readonly ServiceProvider _services;

    private ShopFixture(
        SqliteConnection connection,
        ServiceProvider services,
        AsyncServiceScope scope,
        List<IDomainEvent> dispatched)
    {
        _connection = connection;
        _services = services;
        _scope = scope;
        Dispatched = dispatched;
        Shop = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    }

    /// <summary>
    ///     The unit of work of the use case under test, and the context its handlers share.
    /// </summary>
    public ShopDbContext Shop { get; }

    /// <summary>Every event that reached the handlers, in the order it did.</summary>
    public List<IDomainEvent> Dispatched { get; }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    /// <param name="addHandlers">The handlers of the test, next to the recording one.</param>
    /// <param name="configureOptions">
    ///     Whatever else a test needs on the context, e.g. a different execution strategy.
    /// </param>
    public static async Task<ShopFixture> CreateAsync(
        Action<IServiceCollection>? addHandlers = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        // The database lives as long as the connection does, so the fixture holds it open.
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var dispatched = new List<IDomainEvent>();

        var services = new ServiceCollection()
            .AddDbContext<ShopDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(connection)
                    .AddInterceptors(new DispatchDomainEventsInterceptor(serviceProvider));
                configureOptions?.Invoke(options);
            })
            .AddSingleton(dispatched)
            .AddSingleton(typeof(IDomainEventHandler<>), typeof(RecordingHandler<>));

        addHandlers?.Invoke(services);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();
        var fixture = new ShopFixture(connection, provider, scope, dispatched);
        await fixture.Shop.Database.EnsureCreatedAsync();

        return fixture;
    }

    /// <summary>
    ///     What actually reached the database, read through a context of its own, so nothing the
    ///     use case still holds in memory can make a failed save look like a successful one.
    /// </summary>
    public async Task<(List<Order> Orders, List<Invoice> Invoices)> ReadStoredAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var shop = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        return (await shop.Orders.AsNoTracking().ToListAsync(),
            await shop.Invoices.AsNoTracking().ToListAsync());
    }
}

internal sealed class ShopDbContext(DbContextOptions<ShopDbContext> options)
    : BoundedContextDbContext<ShopDbContext>(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>
    ///     The id of an entity has no setter, so EF Core does not map it by convention. A real
    ///     bounded context names the key in its entity configuration, this is the short form.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>().HasKey(order => order.Id);
        modelBuilder.Entity<Invoice>().HasKey(invoice => invoice.Id);
    }
}

internal sealed record OrderPlaced(Guid OrderId) : IDomainEvent;

internal sealed record InvoiceWritten(Guid InvoiceId, Guid OrderId) : IDomainEvent;

internal sealed class Order : AggregateRoot<Guid>
{
    private Order(Guid id) : base(id)
    {
    }

    /// <summary>Used by EF Core for materialization.</summary>
    private Order()
    {
    }

    public static Order Place()
    {
        var order = new Order(Guid.CreateVersion7());
        order.Raise(new OrderPlaced(order.Id));

        return order;
    }
}

internal sealed class Invoice : AggregateRoot<Guid>
{
    private Invoice(Guid id, Guid orderId) : base(id)
    {
        OrderId = orderId;
    }

    /// <summary>Used by EF Core for materialization.</summary>
    private Invoice()
    {
    }

    public Guid OrderId { get; private set; }

    public static Invoice WriteFor(Guid orderId)
    {
        var invoice = new Invoice(Guid.CreateVersion7(), orderId);
        invoice.Raise(new InvoiceWritten(invoice.Id, orderId));

        return invoice;
    }
}

/// <summary>
///     Hears every event of every type, through the same interface a real handler implements, and
///     keeps it for the assertions.
/// </summary>
internal sealed class RecordingHandler<TDomainEvent>(List<IDomainEvent> dispatched)
    : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public Task HandleAsync(TDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        dispatched.Add(domainEvent);

        return Task.CompletedTask;
    }
}
