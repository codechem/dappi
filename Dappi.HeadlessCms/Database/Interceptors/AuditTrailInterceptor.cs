using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dappi.HeadlessCms.Database.Interceptors
{
    public class AuditTrailInterceptor(ICurrentExternalSessionProvider currentSessionProvider) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (eventData.Context is null)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }
            
            var userId = currentSessionProvider.GetCurrentUserId();
        
            SetAuditableProperties(eventData.Context, userId);

            var auditEntries = HandleAuditingBeforeSaveChanges(eventData.Context, userId).ToList();
            if (auditEntries.Count > 0)
            {
                await eventData.Context.Set<AuditTrail>().AddRangeAsync(auditEntries, cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        
        private void SetAuditableProperties(DbContext context, string? userId)
        {
            const string systemSource = "system";
            foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                        entry.Entity.CreatedBy = userId ?? systemSource;
                        break;

                    case EntityState.Modified:
                        entry.Entity.CreatedAtUtc = entry.OriginalValues["CreatedAtUtc"] as DateTime?;
                        entry.Entity.CreatedBy = entry.OriginalValues["CreatedBy"] as string;
                        entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = userId ?? systemSource;
                        break;
                }
            }
        }
        
        private List<AuditTrail> HandleAuditingBeforeSaveChanges(DbContext context, string? userId)
        {
            var auditableEntries = context.ChangeTracker.Entries<AuditableEntity>()
                .Where(x => x.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
                .Select(x => CreateTrailEntry(userId, x))
                .ToList();

            return auditableEntries;
        }

        private static AuditTrail CreateTrailEntry(string? userId, EntityEntry<AuditableEntity> entry)
        {
            var trailEntry = new AuditTrail
            {
                Id = Guid.CreateVersion7(),
                EntityName = entry.Entity.GetType().Name,
                UserId = userId,
                DateUtc = DateTime.UtcNow
            };

            SetAuditTrailPropertyValues(entry, trailEntry);
            SetAuditTrailNavigationValues(entry, trailEntry);
            SetAuditTrailReferenceValues(entry, trailEntry);

            return trailEntry;
        }
        
        private static void SetAuditTrailPropertyValues(EntityEntry entry, AuditTrail trailEntry)
        {
            // Skip temp fields (that will be assigned automatically by ef core engine, for example: when inserting an entity
            foreach (var property in entry.Properties.Where(x => !x.IsTemporary))
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    trailEntry.EntityId = property.CurrentValue?.ToString();
                    continue;
                }

                // Filter properties that should not appear in the audit list
                if (property.Metadata.Name.Equals("PasswordHash"))
                {
                    continue;
                }

                SetAuditTrailPropertyValue(entry, trailEntry, property);
            }
        }

        private static void SetAuditTrailPropertyValue(EntityEntry entry, AuditTrail trailEntry, PropertyEntry property)
        {
            var propertyName = property.Metadata.Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    trailEntry.TrailType = TrailType.Create;
                    trailEntry.NewValues[propertyName] = property.CurrentValue;
                    
                    break;

                case EntityState.Deleted:
                    trailEntry.TrailType = TrailType.Delete;
                    trailEntry.OldValues[propertyName] = property.OriginalValue;

                    break;

                case EntityState.Modified:
                    if (property.IsModified && (property.OriginalValue is null || !property.OriginalValue.Equals(property.CurrentValue)))
                    {
                        trailEntry.ChangedColumns.Add(propertyName);
                        trailEntry.TrailType = TrailType.Update;
                        trailEntry.OldValues[propertyName] = property.OriginalValue;
                        trailEntry.NewValues[propertyName] = property.CurrentValue;
                    }

                    break;
            }

            if (trailEntry.ChangedColumns.Count > 0)
            {
                trailEntry.TrailType = TrailType.Update;
            }
        }

        private static void SetAuditTrailReferenceValues(EntityEntry entry, AuditTrail trailEntry)
        {
            foreach (var reference in entry.References.Where(x => x.IsModified))
            {
                var referenceName = reference.EntityEntry.Entity.GetType().Name;
                trailEntry.ChangedColumns.Add(referenceName);
            }
        }

        private static void SetAuditTrailNavigationValues(EntityEntry entry, AuditTrail trailEntry)
        {
            foreach (var navigation in entry.Navigations.Where(x => x.Metadata.IsCollection && x.IsModified))
            {
                if (navigation.CurrentValue is not ICollection<object> c)
                {
                    continue;
                }

                var collection = c.ToList();
                if (collection.Count == 0)
                {
                    continue;
                }

                var navigationName = collection.First().GetType().Name;
                trailEntry.ChangedColumns.Add(navigationName);
            }
        }
    }
}