namespace ServiceDesk.Api.Ai;

public class AiOptions
{
    public string Endpoint { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";          // user-secrets locally, Ai__ApiKey on Render
    public string[] Services { get; set; } = [];
}