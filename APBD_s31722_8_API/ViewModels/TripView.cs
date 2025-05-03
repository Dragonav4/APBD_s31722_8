using APBD_s31722_8_API.Datalayer.Models;

namespace APBD_s31722_8_API.ViewModels;

public class TripView
{
    public int IdTrip { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<string> Countries { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }

    public TripView(IEnumerable<TripDto> tripInfo)
    {
        var data = tripInfo.ToList();
        var firstItem = data[0];
        IdTrip = firstItem.IdTrip;
        Name = firstItem.Name;
        Description = firstItem.Description;
        DateFrom = firstItem.DateFrom;
        DateTo = firstItem.DateTo;
        MaxPeople = firstItem.MaxPeople;
        RegisteredAt = firstItem.RegisteredAt;
        PaymentDate = firstItem.PaymentDate;
        Countries = data
            .Select(x => x.Country)
            .Distinct()
            .OrderBy(item => item)
            .ToList();
        
    }
}