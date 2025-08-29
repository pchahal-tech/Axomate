// File: Axomate.Infrastructure/Database/Security/EncryptedStringConverter.cs
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Axomate.Infrastructure.Database.Security
{
    /// <summary>
    /// EF Core converter that encrypts on write and tolerantly decrypts on read.
    /// Uses DpapiCrypt (per-user on Windows; plaintext passthrough elsewhere).
    /// </summary>
    internal sealed class EncryptedStringConverter : ValueConverter<string?, string?>
    {
        public EncryptedStringConverter()
            : base(
                  v => DpapiCrypt.Encrypt(v),
                  v => DpapiCrypt.Decrypt(v))
        { }
    }
}
