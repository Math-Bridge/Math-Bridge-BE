namespace MathBridge.Application.DTOs;

public class AddressAutocompleteResponse
{
    public List<AddressPrediction> Predictions { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AddressPrediction
{
    public string PlaceId { get; set; }
    public string Description { get; set; }
    public string MainText { get; set; }
    public string SecondaryText { get; set; }
    public List<string> Types { get; set; } = new();
}