using APBD_s31722_8_API.Datalayer;
using APBD_s31722_8_API.Datalayer.Models;
using APBD_s31722_8_API.Utils;
using APBD_s31722_8_API.ViewModels;

namespace APBD_s31722_8_API.Services;

public class TripService
{
    private readonly DbClient _dbClient;

    private const string getAllTripsQuery = @"
                SELECT 
                    t.IdTrip,
                    t.Name,
                    t.Description,
                    t.DateFrom,
                    t.DateTo,
                    t.MaxPeople,
                    c.Name AS CountryName
                FROM Trip t
                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                JOIN Country c ON ct.IdCountry = c.IdCountry
                ORDER BY IdTrip ASC";

    
    public TripService(DbClient dbClient)
    {
        _dbClient = dbClient;
    }
    
    public async Task<List<TripView>> GetTripsAsync()
    {
        var tripDtos = await _dbClient
            .ReadDataAsync<TripDto>(getAllTripsQuery, reader => new TripDto(reader))
            .ToListAsync();
        return tripDtos
            .GroupBy(item => item.IdTrip)
            .Select(group => new TripView(group))
            .ToList();
    }
}