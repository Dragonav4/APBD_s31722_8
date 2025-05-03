namespace APBD_s31722_8_API.Models;

public class TripDto
{
    public int IdTrip { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime DateFrom { get; set; }
    public required DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public string Country { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }

    public TripDto() {}
    
}