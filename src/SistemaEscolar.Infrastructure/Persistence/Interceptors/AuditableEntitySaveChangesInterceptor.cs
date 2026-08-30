using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Domain.Common;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return base.SavingChanges(eventData, result);
        }

        UpdateAuditFields(eventData.Context.ChangeTracker);
        CreateAuditEntries(eventData.Context.ChangeTracker);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        UpdateAuditFields(eventData.Context.ChangeTracker);
        CreateAuditEntries(eventData.Context.ChangeTracker);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(ChangeTracker changeTracker)
    {
        var userId = _currentUserService.UserId;

        foreach (var entry in changeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }

    private static void CreateAuditEntries(ChangeTracker changeTracker)
    {
        var trackedEntries = changeTracker
            .Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(x => x.Entity is not AuditLog)
            .ToList();

        foreach (var entry in trackedEntries)
        {
            var audit = new AuditLog
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                Action = entry.State.ToString().ToUpperInvariant(),
                KeyValues = SerializePrimaryKey(entry),
                OldValues = SerializeValues(entry, useOriginalValues: true),
                NewValues = SerializeValues(entry, useOriginalValues: false)
            };

            changeTracker.Context?.Set<AuditLog>().Add(audit);
        }
    }

    private static string? SerializePrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        var dict = new Dictionary<string, object?>();
        foreach (var property in key.Properties)
        {
            dict[property.Name] = entry.Property(property.Name).CurrentValue;
        }

        return JsonSerializer.Serialize(dict);
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginalValues)
    {
        if (entry.State == EntityState.Added && useOriginalValues)
        {
            return null;
        }

        if (entry.State == EntityState.Deleted && !useOriginalValues)
        {
            return null;
        }

        var values = useOriginalValues ? entry.OriginalValues : entry.CurrentValues;
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            dict[property.Metadata.Name] = values[property.Metadata.Name];
        }

        return JsonSerializer.Serialize(dict);
    }
}
