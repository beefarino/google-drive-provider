using CodeOwls.PowerShell.Provider.PathNodes;
using Google.Apis.Drive.v3;

namespace CodeOwls.Google.Drive.Provider
{
    public class GoogleDriveRootNode : GoogleDriveItemPathNode
    {
        private readonly DriveService _service;

        public GoogleDriveRootNode( DriveService service ) : base(service)
        {
            _service = service;
        }
        public override IPathValue GetNodeValue()
        {
            return new ContainerPathValue( _service, Name );
        }

        public override string Name
        {
            get { return _service.Name; }
        }

        override protected string ParentId
        {
            get { return "root"; }
        }
    }
}