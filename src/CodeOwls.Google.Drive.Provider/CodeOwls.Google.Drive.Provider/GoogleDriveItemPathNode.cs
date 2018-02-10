using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using File = Google.Apis.Drive.v3.Data.File;

namespace CodeOwls.Google.Drive.Provider
{
    public class GoogleDriveItemPathNode : PathNodeBase, IGetItemContent
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

        public IContentReader GetContentReader(IProviderContext providerContext)
        {
            var p = providerContext.DynamicParameters as GetContentDynamicParameters;
            var export = _service.Files.Export(_file.Id, p.MimeType);
            
            var stream = new MemoryStream();
            //providerContext.WriteProgress();
            export.Download(stream);
            stream.Position = 0;
            var reader = new GoogleExporedFileContentReader(stream);
            return reader;
        }

        public class GetContentDynamicParameters
        {
            [Parameter(Mandatory = true)]
            public string MimeType { get; set; }
        }
        public object GetContentReaderDynamicParameters(IProviderContext providerContext)
        {
            return new GetContentDynamicParameters();
        }
    }

    public class GoogleExporedFileContentReader : IContentReader
    {
        private Stream _stream;
        
        public GoogleExporedFileContentReader(Stream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            var stream = _stream;
            _stream = null;
            if (null != stream)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }

        public IList Read(long readCount)
        {
            var actualReadCount = readCount > 0 ? readCount : _stream.Length;

            ArrayList list = new ArrayList();

            var buffer = new byte[actualReadCount];
            var thisCount = _stream.Read(buffer, 0, (int) actualReadCount);
            if (thisCount != actualReadCount)
            {
                var thisBuffer = new byte[thisCount];
                Array.Copy(buffer, thisBuffer, thisCount);
                buffer = thisBuffer;
            }
            list.AddRange(buffer);

            return list;
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
        }

        public void Close()
        {
            Dispose();
        }
    }
}