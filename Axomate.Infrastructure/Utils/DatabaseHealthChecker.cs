using System.IO;

namespace Axomate.Infrastructure.Utils
{
    public static class DatabaseHealthChecker
    {
        private const long WarningThresholdBytes = 1L * 1024 * 1024 * 1024; // 1 GB

        /// <summary>
        /// Checks if the database file exceeds the threshold.
        /// </summary>
        public static bool IsDatabaseTooLarge(string dbPath, out long sizeBytes)
        {
            sizeBytes = 0;

            try
            {
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    sizeBytes = fileInfo.Length;

                    return sizeBytes > WarningThresholdBytes;
                }
            }
            catch
            {
                // Fail safe — ignore errors
            }

            return false;
        }
    }
}
