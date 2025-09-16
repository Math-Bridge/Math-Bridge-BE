namespace MathBridge.Application.DTOs;

public class SaveAddressResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime LocationUpdatedDate { get; set; }
}