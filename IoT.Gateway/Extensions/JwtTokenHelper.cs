using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IoT.Gateway.Extensions;

public static class JwtTokenHelper
{
    /// <summary>
    /// Generates a JWT token for a device.
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="scope">Device scope/claim</param>
    /// <param name="secretKey">HMAC secret key</param>
    /// <param name="expiryMinutes">Token expiration in minutes</param>
    /// <returns>JWT string</returns>
    public static string GenerateToken(
        string deviceId,
        string scope,
        string secretKey,
        int expiryMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("JWT secret key must be provided.", nameof(secretKey));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, deviceId),
            new Claim("scope", scope),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "IoT.DeviceGateway",        // optional, can be configured
            audience: "IoT.Devices",           // optional, can be configured
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}