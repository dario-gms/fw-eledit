using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowVersionUiService
    {
        public string InitializeVersion(
            Assembly assembly,
            Label versionLabel,
            NavigationStateService navigationStateService,
            string fallbackVersion)
        {
            string displayVersion = fallbackVersion;
            if (assembly != null)
            {
                try
                {
                    object[] attrs = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        AssemblyInformationalVersionAttribute info = attrs[0] as AssemblyInformationalVersionAttribute;
                        if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion))
                        {
                            displayVersion = info.InformationalVersion.Trim();
                        }
                    }
                    else
                    {
                        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                        if (!string.IsNullOrWhiteSpace(fileVersionInfo.ProductVersion))
                        {
                            displayVersion = fileVersionInfo.ProductVersion;
                        }
                    }
                }
                catch
                {
                }
            }

            if (versionLabel != null)
            {
                versionLabel.Text = "FWEledit v" + displayVersion;
            }

            if (navigationStateService != null)
            {
                try
                {
                    navigationStateService.ResetIfVersionChanged(displayVersion);
                }
                catch
                {
                }
            }

            return displayVersion;
        }
    }
}
