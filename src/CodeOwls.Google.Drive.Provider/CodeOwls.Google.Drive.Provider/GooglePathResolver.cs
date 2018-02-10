using System.Collections.Generic;
using System.Linq;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Google.Apis.Drive.v3;

namespace CodeOwls.Google.Drive.Provider
{
    public class GooglePathResolver : PathResolverBase
    {
        private readonly DriveService _driveService;
        private static Dictionary< string, IEnumerable<IPathNode>> Cache = new Dictionary<string, IEnumerable<IPathNode>>();
        
        public GooglePathResolver( DriveService driveService )
        {
            _driveService = driveService;
        }

        public static void ResetCache()
        {
            Cache.Clear();
        }

        public override IEnumerable<IPathNode> ResolvePath(IProviderContext providerContext, string path)
        {
            if (Cache.ContainsKey(path))
            {
                return Cache[path];
            }
    
            var nodes = base.ResolvePath(providerContext, path);
            var resolvePath = nodes as IList<IPathNode> ?? nodes.ToList();
            Cache[path] = resolvePath;
            return resolvePath;
        }

        protected override IPathNode Root
        {
            get { return new GoogleDriveRootNode(_driveService); }
        }
    }
}