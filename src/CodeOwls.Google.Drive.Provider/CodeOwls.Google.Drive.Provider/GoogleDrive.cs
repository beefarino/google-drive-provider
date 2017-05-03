using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using File = Google.Apis.Drive.v3.Data.File;

namespace CodeOwls.Google.Drive.Provider
{
    public class GoogleDrive : PowerShell.Provider.Drive
    {
        private static string[] Scopes = {DriveService.Scope.DriveReadonly};
        private static string ApplicationName = "PowerShell Provider for Google Drive";

        UserCredential credential;
        public static DriveService driveService;

        private void Authorize()
        {

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            assemblyPath = Path.Combine(assemblyPath, "client_secret.json");
            using (var stream =
                new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/codeowls-google-drive-provider");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);

            }
        }

        internal DriveService CreateDriveService(){
            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        public object GetSheetData(string sheetId, string tabName)
        {
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Spreadsheets.Values.Get(sheetId, tabName + "!A1:Z65535");
            request.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.ROWS;
            var result = request.Execute();
            return result;

        }

        public object GetFilesMatchingName(string name)
        {
            var pageStreamer = new PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                (request, token) => request.PageToken = token,
                response => response.NextPageToken,
                response => response.Files);

            var req = driveService.Files.List();
            req.PageSize = 1000;
            req.OrderBy = "name";
            req.Q = String.Format("name contains '{0}'", name);

            return pageStreamer.Fetch(req);
        }

        public object GetFilesOfMimeType(string mimeType)
        {
            var pageStreamer = new PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                (request, token) => request.PageToken = token,
                response => response.NextPageToken,
                response => response.Files);

            var req = driveService.Files.List();
            req.PageSize = 1000;
            req.OrderBy = "name";
            req.Q = String.Format("mimeType = '{0}'", mimeType);

            return pageStreamer.Fetch(req);
        }
        
        public GoogleDrive(PSDriveInfo driveInfo)
            : base(driveInfo)
        {
            if (null == driveService)
            {
                Authorize();
                driveService = CreateDriveService();
            }
        }
    }
}