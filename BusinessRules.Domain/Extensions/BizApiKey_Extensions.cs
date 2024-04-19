using BusinessRules.Domain.Common;
using BusinessRules.Domain.DTO;
using BusinessRules.Domain.Organization;
using BusinessRules.Domain.Services;

namespace BusinessRules.Domain.Fields
{
    public static class BizApiKey_Extensions
    {
        public static bool CanLogResult(
            this BizApiKey currentApiKey)
        {
            if (!FeatureFlags.OfflineMode)
            {
                return !string.IsNullOrWhiteSpace(currentApiKey.FtpsServer)
                    && !string.IsNullOrWhiteSpace(currentApiKey.FtpsUsername)
                    && !string.IsNullOrWhiteSpace(currentApiKey.FtpsPassword)
                    && !string.IsNullOrWhiteSpace(currentApiKey.FtpsRemoteDirectory)
                    && !string.IsNullOrWhiteSpace(currentApiKey.FtpsPort);
            } else
            {
                return !string.IsNullOrWhiteSpace(currentApiKey.LocalLoggingDirectory);
            }
        }
    }
}
