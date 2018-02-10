using System;
using System.Collections.Generic;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;

namespace CodeOwls.Google.Drive.Provider
{
    public class GoogleDriveItemPathNode : PathNodeBase
    {
        private readonly DriveService _service;
        private readonly File _file;

        protected GoogleDriveItemPathNode(DriveService service ) : this(service,null)
        {
            
        }
        public GoogleDriveItemPathNode(DriveService service, File file)
        {
            _service = service;
            _file = file;
        }


        public override IEnumerable<IPathNode> GetNodeChildren(IProviderContext providerContext)
        {
            var pageStreamer = new PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                (request, token) => request.PageToken = token,
                response => response.NextPageToken,
                response => response.Files);

            var req = _service.Files.List();
            req.PageSize = 1000;
            req.OrderBy = "name";
            req.Q = String.Format("'{0}' in parents", ParentId);
            req.Fields = "nextPageToken,files(id,name,kind,mimeType,createdTime,sharingUser,shared,size,modifiedTime,parents)";
            var folders = new List<IPathNode>();
            var files = new List<IPathNode>();

            foreach (var file in pageStreamer.Fetch(req))
            {
                yield return new GoogleDriveItemPathNode(_service, file);

                //var target = file.MimeType == "application/vnd.google-apps.folder" ? folders : files;
                //target.Add( new GoogleDriveItemPathNode(file) );
            }
        }
 
        public override IPathValue GetNodeValue()
        {
            var pathItem = _file.MimeType == "application/vnd.google-apps.folder" ? 
                (IPathValue) new ContainerPathValue( _file, _file.OriginalFilename ) :
                (IPathValue) new LeafPathValue( _file, _file.OriginalFilename );
            return pathItem;
        }

        public override string Name
        {
            get { return _file.Name; }
        }

        virtual protected string ParentId
        {
            get { return _file.Id; }
        }
    }
}