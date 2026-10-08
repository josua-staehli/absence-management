using System.Data.Common;
using Common.Application.Handlers;
using Common.Domain.Primitives;
using Common.Infrastructure;
using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Common.UnitTests.Infrastructure;

/// <summary>
///     The guarantees of the domain event dispatch, which every bounded context inherits: what the
///     handlers change is stored together with the change that raised the event, or not at all,
///     and no event is dispatched twice - also when a handler breaks the rules, fails or is
///     cancelled, or when the database fails. Each test saves the way a use case does, through the
///     <c>DbContext</c> - see <see cref="ShopFixture" /> for the shop that stands in for a bounded
///     context.
/// </summary>
public class DomainEventDispatchTests
{
    [Fact]
    public async Task Saving_hands_the_events_of_the_saved_aggregates_to_their_handlers()
    {
        await using var fixture = await ShopFixture.CreateAsync();
        var order = Order.Place();

        fixture.Shop.Orders.Add(order);

        // Raised, but not saved yet: nothing has reacted.
        Assert.Empty(fixture.Dispatched);

        await fixture.Shop.SaveChangesAsync();

        Assert.Equal([new OrderPlaced(order.Id)], fixture.Dispatched);
    }

    /// <summary>The reason to dispatch before the commit: one transaction for both.</summary>
    [Fact]
    public async Task What_a_handler_changes_is_stored_with_the_change_that_raised_the_event()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>());
        var order = Order.Place();

        fixture.Shop.Orders.Add(order);
        await fixture.Shop.SaveChangesAsync();

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Equal(order.Id, Assert.Single(orders).Id);
        Assert.Equal(order.Id, Assert.Single(invoices).OrderId);
    }

    /// <summary>
    ///     The other half of one transaction: the change that raised the event is not stored
    ///     either. The exception is the handler's own, so the use case fails with the real cause.
    /// </summary>
    [Fact]
    public async Task When_a_handler_fails_nothing_is_stored()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        await Assert.ThrowsAsync<HandlerFailedException>(() => fixture.Shop.SaveChangesAsync());

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Empty(orders);
        Assert.Empty(invoices);
    }

    /// <summary>
    ///     The invoice the handler writes raises an event of its own. It is dispatched by the same
    ///     save, after the event that caused it.
    /// </summary>
    [Fact]
    public async Task Events_raised_by_a_handler_are_dispatched_by_the_same_save()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>());
        var order = Order.Place();

        fixture.Shop.Orders.Add(order);
        await fixture.Shop.SaveChangesAsync();

        var invoice = Assert.Single((await fixture.ReadStoredAsync()).Invoices);
        Assert.Equal(
            [new OrderPlaced(order.Id), new InvoiceWritten(invoice.Id, order.Id)],
            fixture.Dispatched);
    }

    [Fact]
    public async Task An_event_is_dispatched_once_even_when_the_context_saves_again()
    {
        await using var fixture = await ShopFixture.CreateAsync();
        var order = Order.Place();

        fixture.Shop.Orders.Add(order);
        await fixture.Shop.SaveChangesAsync();
        await fixture.Shop.SaveChangesAsync();

        Assert.Single(fixture.Dispatched);
        Assert.Empty(order.DomainEvents);
    }

    /// <summary>
    ///     A handler that saves would commit the change that raised the event before the other
    ///     handlers had their turn. It is refused, and nothing is stored.
    /// </summary>
    [Fact]
    public async Task A_handler_that_saves_by_itself_is_refused()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, SavingHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Shop.SaveChangesAsync());

        Assert.Contains("must not save", exception.Message);

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Empty(orders);
        Assert.Empty(invoices);
    }

    /// <summary>
    ///     While the handlers run, the order's event has been taken out of it already, so the rule
    ///     against synchronous saves with events to dispatch does not apply. This one does:
    ///     allowed, the save would commit the order before the failing handler had its turn.
    /// </summary>
    [Fact]
    public async Task A_handler_that_saves_synchronously_is_refused_even_without_pending_events()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, SynchronousSavingHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Shop.SaveChangesAsync());

        Assert.Contains("must not save", exception.Message);
        Assert.Empty((await fixture.ReadStoredAsync()).Orders);
    }

    /// <summary>
    ///     A direct write would be committed on its own, before the save and whatever its outcome:
    ///     here the failing handler would leave the stored order deleted although nothing else was
    ///     saved. It is refused, so everything a handler does waits for the save.
    /// </summary>
    [Fact]
    public async Task A_handler_that_writes_to_the_database_directly_is_refused()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, DeleteOrdersHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());
        var stored = await StoreWithoutDispatchingAsync(fixture, Order.Place());

        fixture.Shop.Orders.Add(Order.Place());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Shop.SaveChangesAsync());

        Assert.Contains("directly", exception.Message);
        Assert.Equal(stored.Id, Assert.Single((await fixture.ReadStoredAsync()).Orders).Id);
    }

    /// <summary>
    ///     Rows coming back do not make SQL a read: <c>DELETE ... RETURNING</c> executes like a
    ///     query, whether it returns entities or plain values. It is refused all the same, also
    ///     when the query was compiled before the handlers ran: the check is made on every
    ///     execution, not when the query is translated.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task A_handler_cannot_write_through_a_raw_query_returning_rows(
        bool synchronous, bool entityQuery)
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>>(provider =>
                new DeleteReturningHandler(provider.GetRequiredService<ShopDbContext>(),
                    synchronous, entityQuery))
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());

        // Compile and execute the same query outside dispatch while the table is empty.
        // The guard must apply at execution time even when the query is already cached.
        await new DeleteReturningHandler(fixture.Shop, synchronous, entityQuery)
            .HandleAsync(new OrderPlaced(Guid.Empty));
        var stored = await StoreWithoutDispatchingAsync(fixture, Order.Place());
        fixture.Shop.Orders.Add(Order.Place());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Shop.SaveChangesAsync());

        Assert.Contains("directly", exception.Message);
        Assert.Equal(stored.Id, Assert.Single((await fixture.ReadStoredAsync()).Orders).Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Shop.SaveChangesAsync());
    }

    /// <summary>
    ///     The other side of the guard: what a handler is meant to do still works. It reads with
    ///     LINQ, synchronously or not, gets tracked entities back, and what it adds for them is
    ///     stored by the same save.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handlers_can_read_with_linq_and_change_tracked_entities(bool synchronous)
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>>(provider =>
                new ReadOrdersHandler(provider.GetRequiredService<ShopDbContext>(), synchronous)));
        var stored = await StoreWithoutDispatchingAsync(fixture, Order.Place());
        fixture.Shop.ChangeTracker.Clear();
        var order = Order.Place();
        fixture.Shop.Orders.Add(order);

        await fixture.Shop.SaveChangesAsync();

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Equal(2, orders.Count);
        Assert.Equal(stored.Id, Assert.Single(invoices).OrderId);
    }

    /// <summary>Without the limit, this save would never finish.</summary>
    [Fact]
    public async Task Handlers_that_keep_raising_events_are_stopped()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, PlaceAnotherOrderHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Shop.SaveChangesAsync());

        Assert.Contains(nameof(OrderPlaced), exception.Message);
        Assert.Empty((await fixture.ReadStoredAsync()).Orders);
    }

    /// <summary>
    ///     A synchronous save cannot run the handlers. Committing anyway would store a change whose
    ///     events nobody ever hears about.
    /// </summary>
    [Fact]
    public async Task A_synchronous_save_with_events_to_dispatch_is_refused()
    {
        await using var fixture = await ShopFixture.CreateAsync();

        fixture.Shop.Orders.Add(Order.Place());

        Assert.Throws<InvalidOperationException>(() => fixture.Shop.SaveChanges());
        Assert.Empty((await fixture.ReadStoredAsync()).Orders);
    }

    /// <summary>
    ///     When a handler fails, the handlers before it have changed tracked entities and the ones
    ///     after it have not, while the events are gone from the aggregates. Saving that would
    ///     store half a reaction, so the context refuses - in either kind of save - and names the
    ///     cause.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_dispatch_requires_a_fresh_context_for_any_further_save(
        bool synchronous)
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        var failure = await Assert.ThrowsAsync<HandlerFailedException>(
            () => fixture.Shop.SaveChangesAsync());
        var refusal = synchronous
            ? Assert.Throws<InvalidOperationException>(() => fixture.Shop.SaveChanges())
            : await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Shop.SaveChangesAsync());

        Assert.Contains("fresh context", refusal.Message);
        Assert.Same(failure, refusal.InnerException);

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Empty(orders);
        Assert.Empty(invoices);
    }

    /// <summary>
    ///     A request that is cancelled while the handlers run - the client went away - is a failed
    ///     dispatch like any other.
    /// </summary>
    [Fact]
    public async Task Cancellation_while_handlers_run_stores_nothing_and_prevents_another_save()
    {
        using var cancellation = new CancellationTokenSource();
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddSingleton(cancellation)
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, CancellingHandler>());

        fixture.Shop.Orders.Add(Order.Place());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Shop.SaveChangesAsync(cancellation.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Shop.SaveChangesAsync());

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Empty(orders);
        Assert.Empty(invoices);
    }

    /// <summary>
    ///     The database refuses the write after every handler has run. The invoice the handler
    ///     added is not stored either: the two are written in the same transaction.
    /// </summary>
    [Fact]
    public async Task When_the_database_refuses_the_save_the_handlers_changes_are_not_stored()
    {
        await using var fixture = await ShopFixture.CreateAsync(handlers => handlers
            .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>());
        var stored = await StoreWithoutDispatchingAsync(fixture, Order.Place());
        var invoice = await StoreWithoutDispatchingAsync(fixture, Invoice.WriteFor(stored.Id));
        fixture.Shop.ChangeTracker.Clear();

        // Stored already, so inserting it again fails on the primary key.
        fixture.Shop.Invoices.Add(invoice);
        fixture.Shop.Orders.Add(Order.Place());

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Shop.SaveChangesAsync());

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Equal(stored.Id, Assert.Single(orders).Id);
        Assert.Equal(invoice.Id, Assert.Single(invoices).Id);
    }

    /// <summary>
    ///     PostgreSQL is registered with <c>EnableRetryOnFailure</c>, so a write that fails for a
    ///     moment is tried again. Every handler has run by then and its changes are still tracked:
    ///     the retry writes them without dispatching anything a second time.
    /// </summary>
    [Fact]
    public async Task A_transient_failure_while_writing_is_retried_without_dispatching_again()
    {
        var database = new FirstInsertFails();
        await using var fixture = await ShopFixture.CreateAsync(
            handlers => handlers
                .AddScoped<IDomainEventHandler<OrderPlaced>, WriteInvoiceHandler>(),
            options => options
                .ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>()
                .AddInterceptors(database));
        var order = Order.Place();

        fixture.Shop.Orders.Add(order);
        await fixture.Shop.SaveChangesAsync();

        Assert.True(database.HasFailed);

        var (orders, invoices) = await fixture.ReadStoredAsync();
        Assert.Equal(order.Id, Assert.Single(orders).Id);
        var invoice = Assert.Single(invoices);
        Assert.Equal(
            [new OrderPlaced(order.Id), new InvoiceWritten(invoice.Id, order.Id)],
            fixture.Dispatched);
    }

    /// <summary>
    ///     The fixture adds the interceptor by hand, the bounded contexts get it from
    ///     <see cref="InfrastructureRegistration.AddBoundedContextDbContext{TContext}" />. This
    ///     pins down that they do, without opening the PostgreSQL connection it configures.
    /// </summary>
    [Fact]
    public void The_dbcontext_of_every_bounded_context_dispatches_domain_events()
    {
        using var services = new ServiceCollection()
            .AddBoundedContextDbContext<ShopDbContext>("Host=never-opened")
            .BuildServiceProvider();
        using var scope = services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ShopDbContext>>();
        var interceptors = options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];

        Assert.Single(interceptors.OfType<DispatchDomainEventsInterceptor>());
    }

    /// <summary>
    ///     Stores an aggregate as an earlier use case left it, without its events reaching the
    ///     handlers of the test.
    /// </summary>
    private static async Task<TAggregate> StoreWithoutDispatchingAsync<TAggregate>(
        ShopFixture fixture,
        TAggregate aggregate)
        where TAggregate : class, IAggregateRoot
    {
        aggregate.ClearDomainEvents();
        fixture.Shop.Add(aggregate);
        await fixture.Shop.SaveChangesAsync();

        return aggregate;
    }
}

/// <summary>
///     What a handler is for: when an order is placed, an invoice is written. It only adds it, the
///     save it runs in stores it.
/// </summary>
internal sealed class WriteInvoiceHandler(ShopDbContext shop) : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
    {
        shop.Invoices.Add(Invoice.WriteFor(domainEvent.OrderId));

        return Task.CompletedTask;
    }
}

internal sealed class FailingHandler : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
    {
        throw new HandlerFailedException();
    }
}

internal sealed class HandlerFailedException() : Exception("The handler failed.");

/// <summary>Breaks the rule a handler has to follow: it saves by itself.</summary>
internal sealed class SavingHandler(ShopDbContext shop) : IDomainEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced domainEvent,
        CancellationToken cancellationToken = default)
    {
        shop.Invoices.Add(Invoice.WriteFor(domainEvent.OrderId));

        await shop.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>The same, with the synchronous save.</summary>
internal sealed class SynchronousSavingHandler(ShopDbContext shop)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
    {
        shop.SaveChanges();

        return Task.CompletedTask;
    }
}

/// <summary>Breaks the other rule: it writes to the database directly.</summary>
internal sealed class DeleteOrdersHandler(ShopDbContext shop) : IDomainEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced domainEvent,
        CancellationToken cancellationToken = default)
    {
        await shop.Orders.ExecuteDeleteAsync(cancellationToken);
    }
}

/// <summary>Places another order for every order placed, which is placed in turn.</summary>
internal sealed class PlaceAnotherOrderHandler(ShopDbContext shop)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
    {
        shop.Orders.Add(Order.Place());

        return Task.CompletedTask;
    }
}

/// <summary>Cancels the request it runs in, the way an aborted HTTP request does.</summary>
internal sealed class CancellingHandler(CancellationTokenSource cancellation)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
    {
        cancellation.Cancel();
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}

/// <summary>Stands in for a connection that drops while the first row is written.</summary>
internal sealed class FirstInsertFails : DbCommandInterceptor
{
    public bool HasFailed { get; private set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        FailTheFirstInsert(command);

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        FailTheFirstInsert(command);

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void FailTheFirstInsert(DbCommand command)
    {
        if (HasFailed || !command.CommandText.Contains("INSERT", StringComparison.Ordinal)) return;

        HasFailed = true;

        throw new TransientFailureException();
    }
}

internal sealed class TransientFailureException() : Exception("The connection dropped.");

/// <summary>
///     What <c>EnableRetryOnFailure</c> does for PostgreSQL, for the failure the test simulates.
/// </summary>
internal sealed class RetryingStrategyFactory(ExecutionStrategyDependencies dependencies)
    : IExecutionStrategyFactory
{
    public IExecutionStrategy Create()
    {
        return new RetryingStrategy(dependencies);
    }
}

internal sealed class RetryingStrategy(ExecutionStrategyDependencies dependencies)
    : ExecutionStrategy(dependencies, 3, TimeSpan.Zero)
{
    protected override bool ShouldRetryOn(Exception exception)
    {
        return exception is TransientFailureException;
    }
}

/// <summary>Breaks the rule in disguise: a delete that returns rows, executed as a query.</summary>
internal sealed class DeleteReturningHandler(ShopDbContext shop, bool synchronous, bool entityQuery)
    : IDomainEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced domainEvent,
        CancellationToken cancellationToken = default)
    {
        if (entityQuery)
        {
            var query = shop.Orders.FromSqlRaw("DELETE FROM \"Orders\" RETURNING \"Id\"");
            if (synchronous)
                query.ToList();
            else
                await query.ToListAsync(cancellationToken);
        }
        else
        {
            var query = shop.Database.SqlQueryRaw<int>(
                "DELETE FROM \"Orders\" RETURNING 1 AS \"Value\"");
            if (synchronous)
                query.ToList();
            else
                await query.ToListAsync(cancellationToken);
        }
    }
}

/// <summary>Does what a handler should: reads with LINQ, and adds to what it read.</summary>
internal sealed class ReadOrdersHandler(ShopDbContext shop, bool synchronous)
    : IDomainEventHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced domainEvent,
        CancellationToken cancellationToken = default)
    {
        var query = shop.Orders.Where(order => order.Id != domainEvent.OrderId);
        var count = synchronous ? query.Count() : await query.CountAsync(cancellationToken);
        var stored = synchronous ? query.Single() : await query.SingleAsync(cancellationToken);
        Assert.Equal(1, count);
        Assert.Equal(EntityState.Unchanged, shop.Entry(stored).State);
        shop.Invoices.Add(Invoice.WriteFor(stored.Id));
    }
}
