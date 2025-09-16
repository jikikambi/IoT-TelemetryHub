namespace IoT.Shared.Helper;

public class CertPasswords
{
    /// <summary>
    /// Base password used for gateway, CA, and device derivation.
    /// Example: "passW0rd"
    /// </summary>
    public string BasePassword { get; set; } = "passW0rd";
}