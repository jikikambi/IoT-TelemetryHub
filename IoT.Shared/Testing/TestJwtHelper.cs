using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IoT.Shared.Testing;

public static class TestJwtHelper
{
    /// <summary>
    /// Generates a minimal, valid JWT for integration tests, mimicking production.
    /// </summary>
    /// <param name="deviceId">Device identifier (sub claim)</param>
    /// <returns>JWT string</returns>
    public static string CreateFakeJwt(string deviceId, TimeSpan? expiresIn = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, deviceId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var now = DateTime.UtcNow;
        var expires = now.Add(expiresIn ?? TimeSpan.FromSeconds(1)); // default 1s

        var token = new JwtSecurityToken(
            issuer: "IoT.DeviceGateway",
            audience: "IoT.DeviceApp",
            claims: claims,
            notBefore: now,
            expires: expires
        );

        return handler.WriteToken(token);
    }
}