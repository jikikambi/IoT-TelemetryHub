namespace IoT.Gateway.Application;

public class JwtSettings
{
    public string Secret { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 60;
}