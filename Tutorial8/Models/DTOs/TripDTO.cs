namespace Tutorial8.Models.DTOs;

/// DTO for returning trip details including associated countries
public class TripDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDTO> Countries { get; set; }
}

/// DTO for country data linked to a trip.
public class CountryDTO
{
    public string Name { get; set; }
}