using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Property;

public class RoomPricingHistory
{
    [Key] 
    public long PriceId { get; set; }

    public int RoomId { get; set; }

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }

    public DateTime ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? Note { get; set; }

    public Room Room { get; set; } = null!;
}