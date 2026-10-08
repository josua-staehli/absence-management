using System.Data.Common;
using System.Reflection;
using Common.Application.Handlers;
using Common.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Database;

/// <summary>
///     Hands the domain events of the aggregates a unit of work saves to their handlers - right
///     before EF Core writes, so that what the handlers change is written in the same transaction
///     as the change that raised the event. If a handler fails, the save fails, and nothing is
///     written at all.
///     <para>
///         That holds because a handler reads with LINQ and changes tracked entities only. SQL it
///         runs itself - <c>ExecuteUpdate</c>, <c>ExecuteDelete</c>, raw SQL - could write, and
///         that write would be committed on its own, before the save and whatever its outcome, so
///         it is refused while the handlers run. Everything a handler does therefore waits in the
///         change tracker until the save writes it, and the execution strategy can retry that
///         write after a transient failure without dispatching anything a second time.
///     </para>
///     <para>
///         <see cref="InfrastructureRegistration.AddBoundedContextDbContext{TContext}" /> adds one
///         to the <c>DbContext</c> of every bounded context, so a use case dispatches the events of
///         its aggregates by saving them, without knowing that there are any.
///     </para>
/// </summary>
/// <param name="serviceProvider">
///     The scope the <c>DbContext</c> belongs to. The handlers are resolved from it, which is how
///     they get the very same <c>DbContext</c> instance.
/// </param>
public sealed class DispatchDomainEventsInterceptor(IServiceProvider serviceProvider)
    : SaveChangesInterceptor, IDbCommandInterceptor
{
    /// <summary>
    ///     A real chain of events is a few rounds deep. Events that are still being raised after
    ///     this many are a cycle, which would otherwise keep the request busy forever.
    /// </summary>
    private const int MaxRounds = 10;

    private static readonly MethodInfo DispatchToHandlersMethod =
        typeof(DispatchDomainEventsInterceptor).GetMethod(
            nameof(DispatchToHandlersAsync),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Set when a handler failed. Some handlers had reacted by then and others had not, so what
    ///     is tracked is half a reaction, and no later save of this context may store it.
    /// </summary>
    private Exception? _dispatchFailure;

    private bool _isDispatching;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return result;
    }

    /// <summary>
    ///     Handlers are asynchronous, so a synchronous save cannot run them. It is refused, rather
    ///     than allowed to commit a change whose events nobody gets to hear about.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureSavingIsSafe();

        if (eventData.Context is not null && HasDomainEvents(eventData.Context))
            throw new InvalidOperationException(
                "The aggregates have domain events to dispatch, which needs SaveChangesAsync.");

        return result;
    }

    public InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureHandlersRunOnlyLinqQueries(eventData);

        return result;
    }

    public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureHandlersRunOnlyLinqQueries(eventData);

        return ValueTask.FromResult(result);
    }

    public InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        EnsureHandlersRunOnlyLinqQueries(eventData);

        return result;
    }

    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        EnsureHandlersRunOnlyLinqQueries(eventData);

        return ValueTask.FromResult(result);
    }

    private async Task DispatchDomainEventsAsync(
        DbContext context,
        CancellationToken cancellationToken)
    {
        EnsureSavingIsSafe();

        _isDispatching = true;

        try
        {
            var round = 0;

            // Handlers may raise events of their own, which are dispatched in the next round, until
            // no aggregate has anything left to report.
            while (TakeDomainEvents(context) is { Count: > 0 } domainEvents)
            {
                if (++round > MaxRounds)
                {
                    var eventTypes = domainEvents
                        .Select(domainEvent => domainEvent.GetType().Name)
                        .Distinct();

                    throw new InvalidOperationException(
                        $"Domain events were still being raised after {MaxRounds} rounds, the "
                        + $"handlers keep raising each other's: {string.Join(", ", eventTypes)}.");
                }

                foreach (var domainEvent in domainEvents)
                    await DispatchAsync(domainEvent, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _dispatchFailure = exception;

            throw;
        }
        finally
        {
            _isDispatching = false;
        }
    }

    private void EnsureSavingIsSafe()
    {
        // A handler that saves would commit the change that raised the event before the other
        // handlers had their turn - in the middle of this very save.
        if (_isDispatching)
            throw new InvalidOperationException(
                "A domain event handler must not save. Its changes are written by the save it runs "
                + "in, together with the change that raised the event.");

        if (_dispatchFailure is not null)
            throw new InvalidOperationException(
                "A domain event handler failed while this context was saving, so its changes are "
                + "incomplete. Retry the use case with a fresh context instead of saving again.",
                _dispatchFailure);
    }

    /// <summary>
    ///     While the handlers run, EF Core may execute LINQ queries for them and nothing else. SQL
    ///     of a handler's own can write, and that write would be committed by itself, outside the
    ///     save: <c>ExecuteUpdate</c>, <c>ExecuteDelete</c> and <c>ExecuteSql</c> obviously, but a
    ///     query in SQL as well - <c>DELETE ... RETURNING</c> returns rows just like a
    ///     <c>SELECT</c>. A raw query composed with LINQ counts as LINQ and is let through: EF Core
    ///     composes only over SQL that starts with <c>SELECT</c>, and refuses anything else.
    /// </summary>
    private void EnsureHandlersRunOnlyLinqQueries(CommandEventData eventData)
    {
        if (_isDispatching && eventData.CommandSource != CommandSource.LinqQuery)
            throw new InvalidOperationException(
                "A domain event handler must not run SQL directly - ExecuteUpdate, ExecuteDelete, "
                + "ExecuteSql, FromSql or SqlQuery - because it could write outside the save. Read "
                + "with LINQ and change tracked entities, the save writes them together with the "
                + "change that raised the event.");
    }

    private static bool HasDomainEvents(DbContext context)
    {
        return context.ChangeTracker.Entries<IAggregateRoot>()
            .Any(entry => entry.Entity.DomainEvents.Count > 0);
    }

    /// <summary>
    ///     Takes the events out of the aggregates, so that saving again does not dispatch them a
    ///     second time.
    /// </summary>
    private static List<IDomainEvent> TakeDomainEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker.Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();

        return domainEvents;
    }

    /// <summary>
    ///     Handlers are registered per event type, and the type of an event is only known at
    ///     runtime. One reflection call gets from there to the typed handlers.
    /// </summary>
    private Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return (Task)DispatchToHandlersMethod
            .MakeGenericMethod(domainEvent.GetType())
            .Invoke(this, [domainEvent, cancellationToken])!;
    }

    private async Task DispatchToHandlersAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>())
            await handler.HandleAsync(domainEvent, cancellationToken);
    }
}
