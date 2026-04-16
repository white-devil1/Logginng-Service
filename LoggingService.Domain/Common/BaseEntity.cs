namespace LoggingService.Domain.Common;
// Logs are immutable — no UpdatedAt, no soft delete
// Once written they are never modified — only deleted by cleanup job
public abstract class BaseEntity<TKey> : IEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public abstract class BaseEntity : BaseEntity<Guid>, IEntity { }
