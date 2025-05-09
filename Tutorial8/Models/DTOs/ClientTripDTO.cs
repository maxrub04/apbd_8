namespace Tutorial8.Models.DTOs;

/// DTO representing a trip registered by a client
public class ClientTripDTO
{
    public string TripName { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string? PaymentDate { get; set; }
}