namespace WebAppTemplate.Shared.Http.Responses.Auth;

public record AuthClaimResponse
{
    // ReSharper disable once UnusedMember.Global
    // Its used by the json serializer ^^
    public AuthClaimResponse()
    {
        
    }

    public AuthClaimResponse(string type, string value)
    {
        Type = type;
        Value = value;
    }
    
    public string Type { get; set; }
    public string Value { get; set; }
}