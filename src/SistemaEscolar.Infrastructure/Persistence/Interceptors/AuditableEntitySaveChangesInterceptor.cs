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
    // Tabelas do Identity que só geram ruído (tokens, logins externos, claims e cadastro de perfis).
    // Ficam registradas as tabelas AspNetUsers (contas) e AspNetUserRoles (perfis de cada conta).
    private static readonly HashSet<string> TabelasIgnoradas = new(StringComparer.Ordinal)
    {
        "AspNetUserTokens", "AspNetUserLogins", "AspNetUserClaims", "AspNetRoleClaims", "AspNetRoles"
    };

    // Campos técnicos: mudam em toda gravação e não representam uma alteração de negócio.
    private static readonly HashSet<string> CamposTecnicos = new(StringComparer.Ordinal)
    {
        nameof(BaseEntity.CreatedAtUtc), nameof(BaseEntity.CreatedBy),
        nameof(BaseEntity.UpdatedAtUtc), nameof(BaseEntity.UpdatedBy)
    };

    // Campos de controle do Identity (contadores de login, carimbos de segurança). Nunca são gravados:
    // um login bem-sucedido ou falho os altera e encheria a auditoria de eventos sem valor.
    private static readonly HashSet<string> CamposIgnoradosDoIdentity = new(StringComparer.Ordinal)
    {
        "SecurityStamp", "ConcurrencyStamp", "AccessFailedCount", "LockoutEnd", "LockoutEnabled",
        // Derivados ou sem valor de auditoria: normalizações de login/e-mail e confirmações padrão da conta.
        "NormalizedUserName", "NormalizedEmail", "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled"
    };

    // Campos sensíveis: a auditoria registra que houve troca, mas nunca o conteúdo.
    private static readonly HashSet<string> CamposSensiveis = new(StringComparer.Ordinal)
    {
        "PasswordHash"
    };

    public const string ValorOculto = "[oculto]";
    public const string ValorAlterado = "[alterado]";

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

    private void CreateAuditEntries(ChangeTracker changeTracker)
    {
        var trackedEntries = changeTracker
            .Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(x => x.Entity is not AuditLog)
            .ToList();

        var userId = Truncate(_currentUserService.UserId, 128);
        var userName = Truncate(_currentUserService.UserName, 64);
        var userFullName = Truncate(_currentUserService.FullName, 200);

        foreach (var entry in trackedEntries)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            if (TabelasIgnoradas.Contains(tableName))
            {
                continue;
            }

            var changedProperties = entry.State == EntityState.Modified
                ? GetChangedProperties(entry)
                : null;

            // Gravação sem nenhuma alteração real (só os campos técnicos mudaram): não há o que auditar.
            if (changedProperties is { Count: 0 })
            {
                continue;
            }

            var audit = new AuditLog
            {
                TableName = tableName,
                Action = entry.State.ToString().ToUpperInvariant(),
                KeyValues = SerializePrimaryKey(entry),
                OldValues = SerializeValues(entry, useOriginalValues: true, changedProperties),
                NewValues = SerializeValues(entry, useOriginalValues: false, changedProperties),
                CreatedBy = userId,
                UserName = userName,
                UserFullName = userFullName
            };

            changeTracker.Context?.Set<AuditLog>().Add(audit);
        }
    }

    // Nomes das propriedades cujo valor realmente mudou, sem contar campos técnicos nem de controle do Identity.
    private static HashSet<string> GetChangedProperties(EntityEntry entry)
    {
        var changed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey()
                || CamposTecnicos.Contains(name)
                || CamposIgnoradosDoIdentity.Contains(name))
            {
                continue;
            }

            if (!Equals(property.OriginalValue, property.CurrentValue))
            {
                changed.Add(name);
            }
        }

        return changed;
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

    private static string? SerializeValues(EntityEntry entry, bool useOriginalValues, HashSet<string>? changedProperties)
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
            var name = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey() || CamposIgnoradosDoIdentity.Contains(name))
            {
                continue;
            }

            if (CamposSensiveis.Contains(name))
            {
                var houveTroca = changedProperties?.Contains(name) == true;
                dict[name] = values[name] is null
                    ? null
                    : (houveTroca && !useOriginalValues ? ValorAlterado : ValorOculto);
                continue;
            }

            dict[name] = values[name];
        }

        return JsonSerializer.Serialize(dict);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : (value.Length <= maxLength ? value : value[..maxLength]);
}
