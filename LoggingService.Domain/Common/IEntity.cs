namespace LoggingService.Domain.Common;

public interface IEntity<TKey> { TKey Id { get; set; } DateTime CreatedAt { get; set; } }
public interface IEntity : IEntity<Guid> { }
