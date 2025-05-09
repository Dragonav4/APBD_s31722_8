using System.Globalization;
using Microsoft.Data.SqlClient;

namespace APBD_s31722_8_API.Datalayer.Models;

public class TripDto
{
    public int IdTrip { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public string Country { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }

    public TripDto()
    {
        
    }
    
    public TripDto(SqlDataReader reader)
    {
        IdTrip = (int)reader["IdTrip"];
        Name = reader["Name"].ToString()!;
        Description = reader["Description"].ToString();
        DateFrom = (DateTime)reader["DateFrom"];
        DateTo = (DateTime)reader["DateTo"];
        MaxPeople = (int)reader["MaxPeople"];
        Country = reader["CountryName"].ToString()!;
        RegisteredAt = TryParseDateTime(reader, "RegisteredAt");
        PaymentDate = TryParseDateTime(reader, "PaymentDate");
    }

    private static DateTime? TryParseDateTime(SqlDataReader reader, string fieldName)
    {
        try
        {
            var fieldValue = reader[fieldName].ToString();
            return DateTime.ParseExact(fieldValue, "yyyyMMdd", CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
    
    
}