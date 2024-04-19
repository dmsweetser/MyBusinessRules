using BusinessRules.Domain.Common;
using System.Security.Cryptography;
using System.Text;

namespace BusinessRules.Licensing
{
    public class LicenseManager
    {
        private readonly string Version = "{EFB59289-0003-4438-8344-44672FFA39A8}";
        public const string CurrentVersionNumber = "4.1.004";

        private string GetKey()
        {
            return Version + CurrentVersionNumber;
        }

        private static readonly Func<DateTime, bool> _allowOfflineMode = 
            (dateToVerify) => dateToVerify >= DateTime.Now.AddDays(-30) && !FeatureFlags.OfflineMode;

        private void UseVersion() => _ = Version;

        public LicenseManager()
        {
            UseVersion();
            BaseAllowOfflineMode = _allowOfflineMode.Invoke;
        }

        public Func<DateTime, bool> AllowOfflineMode => BaseAllowOfflineMode;

        public static Func<DateTime, bool> BaseAllowOfflineMode { get; set; }


        public byte[] EncryptCompanyId(Guid companyId)
        {
            UseVersion();

            byte[] keyBytes = Encoding.UTF8.GetBytes(GetKey());

            byte[] key = SHA256.Create().ComputeHash(keyBytes);

            byte[] iv = new byte[16];
            RandomNumberGenerator.Create().GetBytes(iv);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                var data = companyId.ToByteArray();

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public Guid DecryptCompanyId(byte[] encryptedValue)
        {
            UseVersion();

            byte[] keyBytes = Encoding.UTF8.GetBytes(GetKey());

            byte[] key = SHA256.Create().ComputeHash(keyBytes);

            byte[] iv = new byte[16];
            RandomNumberGenerator.Create().GetBytes(iv);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                var data = encryptedValue;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return new Guid(decryptor.TransformFinalBlock(data, 0, data.Length));
                }
            }
        }
    }
}