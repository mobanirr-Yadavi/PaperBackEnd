namespace PaperSite.Domain.Common;

/// <summary>
/// Base entity used by all aggregate roots and child entities.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
}
