using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;

using Dropbox.Api;
using Dropbox.Api.Files;
using Newtonsoft.Json.Linq;

namespace Cloud_Manager.Managers
{
    class DropboxManager : CloudDrive
    {
        #region Variables
        private DropboxClient _dbx;
        private readonly string _pathName;
        private string _appKey;
        private string _loopbackHost;
        private Uri _redirectUri;
        private Uri _jsRedirectUri;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <c>DropboxManager</c> class
        /// </summary>
        /// <param name="name">Name of the cloud</param>
        public DropboxManager(string name)
        {
            GetAppInfo();
            _pathName = "profile\\" + name + ".token_response";
            if (File.Exists(_pathName))
            {
                GetCredentials();
            }
            else
            {
                var task = Task.Run(() => Authorize());
                task.Wait();
                SaveCredentials(task.Result);
                _dbx = new DropboxClient(task.Result);
                
            }
        }
        #endregion

        #region Properties
        #endregion

        #region Methods

        /// <summary>
        /// Saves user token into a file for a further login w/o using a browser.
        /// </summary>
        /// <param name="token">User token</param>
        private void SaveCredentials(string token)
        {
            using (var stream = new FileStream(_pathName, FileMode.Create)) 
            {
                string cred = "{\"token\" : \"" + token + "\"}";

                byte[] array = System.Text.Encoding.Default.GetBytes(cred);
                stream.Write(array, 0, array.Length);
            }
        }

        /// <summary>
        /// Gets user token from a file. 
        /// </summary>
        private void GetCredentials()
        {
            using (var stream = new FileStream(_pathName, FileMode.Open, FileAccess.Read))
            {
                byte[] array = new byte[stream.Length];
                stream.Read(array, 0, array.Length);
                string text = System.Text.Encoding.Default.GetString(array);
                dynamic json = JObject.Parse(text);
                string token = json.token;
                _dbx = new DropboxClient(token);
            }
        }

        /// <summary>
        /// Gets an application information from a file. It is needed for requests to user files.
        /// </summary>
        private void GetAppInfo()
        {
            using (var stream = new FileStream("client_secret_dropbox.json", FileMode.Open, FileAccess.Read))
            {
                byte[] array = new byte[stream.Length];
                stream.Read(array, 0, array.Length);
                string textFromFile = System.Text.Encoding.Default.GetString(array);
                dynamic json = JObject.Parse(textFromFile);
                _appKey = json.app_key;
                _loopbackHost = json.loopback_host;
                _redirectUri = new Uri(_loopbackHost + json.redirect_uri);
                _jsRedirectUri = new Uri(_loopbackHost + json.js_redirect_uri);
            }
            
        }

        /// <summary>
        /// Authorizes an application to work with user data. Provides via a browser.
        /// </summary>
        /// <returns></returns>
        public async Task<string> Authorize()
        {
            var state = Guid.NewGuid().ToString("N");
            var authUri = DropboxOAuth2Helper.GetAuthorizeUri(
                OAuthResponseType.Token,
                _appKey,
                _redirectUri,
                state: state);
            OAuth2Response result;
            using (var http = new HttpListener())
            {
                http.Prefixes.Add(_loopbackHost);
                http.Start();

                System.Diagnostics.Process.Start(authUri.ToString());

                await HandleOAuth2Redirect(http).ConfigureAwait(false);

                // Handle redirect from JS and process OAuth response.
                result = await HandleJsRedirect(http).ConfigureAwait(false);

            }



            if (result.State != state)
            {
                // The state in the response doesn't match the state in the request.
                return null;
            }

            return result.AccessToken;
        }

        private async Task HandleOAuth2Redirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            // We only care about request to RedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != _redirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            // Respond with a HTML page which runs JS to send URl fragment.
            context.Response.ContentType = "text/html";

            // Respond with a page which runs JS and sends URL fragment as query string
            // to TokenRedirectUri.
            using (var file = File.OpenRead("index.html"))
            {
                file.CopyTo(context.Response.OutputStream);
            }

            context.Response.OutputStream.Close();
        }

        private async Task<OAuth2Response> HandleJsRedirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            // We only care about request to TokenRedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != _jsRedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            var redirectUri = new Uri(context.Request.QueryString["url_with_fragment"]);

            var result = DropboxOAuth2Helper.ParseTokenFragment(redirectUri);

            return result;
        }

        static async Task Download(string name, string id, DropboxClient dbx)
        {
            var items = await dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var item in items.Entries)
            {
                if (item.IsFile && item.AsFile.Id == id)
                {
                    using (var response = await dbx.Files.DownloadAsync(item.PathDisplay))
                    {
                        var s = response.GetContentAsByteArrayAsync();
                        s.Wait();
                        var d = s.Result;
                        File.WriteAllBytes(name, d);
                    }

                }
            }
        }

        /// <summary>
        /// Downloads a files which id equals parameter id.
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <param name="id">The id of the file</param>
        /// <returns></returns>
        public override void DownloadFile(string name, string id)
        {
            var saveDialog = new SaveFileDialog
            {
                FileName = name,
                Filter = "All files (*.*)|*.*"
            };
            if (saveDialog.ShowDialog() != true) { return;}

            var downloadFileName = saveDialog.FileName;
            var task = Task.Run(() => Download(downloadFileName, id, _dbx));
            task.Wait();
        }

        static async Task Upload(string file, string content, DropboxClient dbx)
        {
            using (var mem = new MemoryStream(File.ReadAllBytes(content)))
            {
                await dbx.Files.UploadAsync(
                    file,
                    WriteMode.Overwrite.Instance,
                    body: mem);
            }
        }

        /// <summary>
        /// Uploads a file into specified directory.
        /// </summary>
        /// <param name="curDir">A directory, where the file will be uploaded</param>
        /// <returns></returns>
        public override void UploadFile(FileStructure curDir)
        {
            var openFileDialog = new OpenFileDialog {Filter = "All files (*.*)|*.*", FileName = ""};
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                fileName = fileName.Substring(fileName.LastIndexOf('\\', fileName.Length - 2) + 1);
                fileName = curDir.Path + '/' + fileName;
                var task = Task.Run(() => Upload(fileName, openFileDialog.FileName, _dbx));
                task.Wait();
            }
        }

        /// <summary>
        /// Paste files into specified directory.
        /// </summary>
        /// <param name="cutFiles">A list of files, which will be pasted</param>
        /// <param name="curDir">A directory, where files will be pasted</param>
        public override void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir)
        {
            string path = curDir.Path;

            var list = _dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var item in cutFiles)
            {
                foreach (var listItem in list.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == item.Id) ||
                        (listItem.IsFolder && listItem.AsFolder.Id == item.Id))
                    {
                        _dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + listItem.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="name">A name of the new directory</param>
        /// <param name="parentDir">A directory, where the new folder will be created</param>
        public override void CreateFolder(string name, FileStructure parentDir)
        {
            string path = parentDir.Path;
            if (path == "/Dropbox")
            {
                _dbx.Files.CreateFolderV2Async("/" + name);
            }
            else
            {
                path = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1);
                path = path.Substring(path.IndexOf("/", StringComparison.Ordinal));
                _dbx.Files.CreateFolderV2Async(path + "/" + name);
            }
        }

        /// <summary>
        /// Moves files into the trash directory.
        /// </summary>
        /// <param name="selectedFiles">A list of files, that will be deleted (into the trash)</param>
        public override void RemoveFile(ICollection<FileStructure> selectedFiles)
        {
            var list = _dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var selectedItem in selectedFiles)
            {
                foreach (var listItem in list.Result.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == selectedItem.Id) ||
                        (listItem.IsFolder && listItem.AsFolder.Id == selectedItem.Id))
                    {
                        _dbx.Files.DeleteV2Async(listItem.PathDisplay);
                    }
                }
            }

        }

        /// <summary>
        /// Moves files into the trash directory.
        /// </summary>
        /// <param name="selectedFiles">A list of files, that will be deleted (into the trash)</param>
        public override void TrashFile(ICollection<FileStructure> selectedFiles)
        {
            RemoveFile(selectedFiles);
        }

        /// <summary>
        /// There is no access to trash from .NET API
        /// </summary>
        [Obsolete]
        public override void UnTrashFile(ICollection<FileStructure> selectedFiles)
        {

        }


        /// <summary>
        /// There is no access to trash from .NET API
        /// </summary>
        [Obsolete]
        public override void ClearTrash()
        {

        }

        /// <summary>
        /// Renames a file.
        /// </summary>
        /// <param name="selectedFiles">Selected file that will be renamed</param>
        /// <param name="newName">A new name of the selected file.</param>
        public override void RenameFile(ICollection<FileStructure> selectedFiles, string newName)
        {
            var list = _dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var listItem in list.Entries)
            {
                if ((listItem.IsFile && listItem.AsFile.Id == selectedFiles.First().Id)
                    || (listItem.IsFolder && listItem.AsFolder.Id == selectedFiles.First().Id))
                {
                    var path = listItem.PathDisplay;
                    path = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));
                    if (listItem.Name.IndexOf('.') >= 0)
                    {
                        newName += listItem.Name.Substring(listItem.Name.LastIndexOf('.'));
                    }
                    _dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + newName);
                }
            }
        }

        /// <summary>
        /// Gets file information.
        /// </summary>
        /// <returns>A list of files.</returns>
        public override ObservableCollection<FileStructure> GetFiles()
        {
            var task = Task.Run(() => GetFolderFiles());
            task.Wait();
            var files = task.Result;
            
            return FileStructure.Convert(files);
        }

        private async Task<List<Metadata>> GetFolderFiles(string path = "")
        {
            ListFolderResult result = await _dbx.Files.ListFolderAsync(path, true);
            return new List<Metadata>(result.Entries);
        }
    }


    #endregion
}
