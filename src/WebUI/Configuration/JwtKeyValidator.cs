using System.Text;

namespace gis_photo_sharing_app.WebUI.Configuration;

public static class JwtKeyValidator
{
    private static readonly string[] PredictableFragments =
    {
        "replace", "change-me", "changeme", "example", "password", "development", "dev-secret", "your-secret",
    };

    public static bool TryValidateProductionKey(string? key, out string reason)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            reason = "The value is missing.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            reason = "Use at least 32 random bytes.";
            return false;
        }

        if (PredictableFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Placeholder and development values are rejected.";
            return false;
        }

        if (key.Distinct().Count() < 10)
        {
            reason = "The value does not contain enough character diversity for a generated secret.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
