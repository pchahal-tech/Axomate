using System.Reflection;

namespace Axomate.UI
{
    public static class AppInfo
    {
        public static string Version
        {
            get
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

                // Prefer InformationalVersion (can include semver), then FileVersion, then AssemblyVersion
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info)) return info;

                var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                if (!string.IsNullOrWhiteSpace(file)) return file;

                return asm.GetName().Version?.ToString() ?? "unknown";
            }
        }
    }
}
