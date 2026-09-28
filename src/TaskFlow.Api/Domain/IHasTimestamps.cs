namespace TaskFlow.Api.Domain;

/// <summary>Entities whose CreatedDate/ModifiedDate are maintained by <see cref="Data.AppDbContext"/>.</summary>
public interface IHasTimestamps
{
    DateTime CreatedDate { get; set; }
    DateTime ModifiedDate { get; set; }
}
