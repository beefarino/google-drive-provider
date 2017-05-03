using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using CodeOwls.PowerShell.Paths.Processors;

namespace CodeOwls.Google.Drive.Provider
{
    [CmdletProvider("GoogleDrive", 
        ProviderCapabilities.ShouldProcess |
        ProviderCapabilities.Include |
        ProviderCapabilities.Filter)]
    public class GoogleProvider : PowerShell.Provider.Provider
    {
        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            var psobject = this.SessionState.InvokeCommand.InvokeScript("$host.runspace").FirstOrDefault();
            var runspace = (Runspace) psobject.BaseObject;
            runspace.AvailabilityChanged += (s, a) =>
            {
                if (a.RunspaceAvailability == RunspaceAvailability.Available)
                {
                    GooglePathResolver.ResetCache();
                }
            };

            return base.Start(providerInfo);
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            if (drive is GoogleDrive)
            {
                return drive;
            }

            return new GoogleDrive(drive);
        }

        protected override IPathResolver PathResolver
        {
            get { return new GooglePathResolver(GoogleDrive.driveService); }
        }
    }
}
