using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using Cloud_Manager.Properties;
using Microsoft.Win32;

using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Common;
using Dropbox.Api.Team;
using Newtonsoft.Json.Linq;

namespace Cloud_Manager.Managers
{
    class DropboxManager : CloudDrive
    {
        #region Variables
        static DropboxClient _dbx;
        private string _appKey;
        private string _loopbackHost;
        private Uri _redirectUri;
        private Uri _jsRedirectUri;
        #endregion

        #region Constructor
        public DropboxManager()
        {
            GetAppInfo();
            var task = Task.Run(() => Authorize());
            task.Wait();
            _dbx = new DropboxClient(task.Result);
        }
        #endregion

        #region Properties
        #endregion

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

        public async Task<string> Authorize()
        {
            var state = Guid.NewGuid().ToString("N");
            var authUri = DropboxOAuth2Helper.GetAuthorizeUri(
                OAuthResponseType.Token,
                _appKey,
                _redirectUri,
                state: state);
            var http = new HttpListener();
            http.Prefixes.Add(_loopbackHost);
            http.Start();

            System.Diagnostics.Process.Start(authUri.ToString());

            await HandleOAuth2Redirect(http);

            // Handle redirect from JS and process OAuth response.
            var result = await HandleJsRedirect(http);

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

        static async Task Download(string name, string id)
        {
            var items = await _dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var item in items.Entries)
            {
                if (item.IsFile && item.AsFile.Id == id)
                {
                    using (var response = await _dbx.Files.DownloadAsync(item.PathDisplay))
                    {
                        var s = response.GetContentAsByteArrayAsync();
                        s.Wait();
                        var d = s.Result;
                        File.WriteAllBytes(name, d);
                    }

                }
            }
        }

        public override void DownloadFile(string name, string id)
        {
            var saveDialog = new SaveFileDialog()
            {
                FileName = name,
                Filter = "All files (*.*)|*.*"
            };
            if (saveDialog.ShowDialog() != true) return;

            var downloadFileName = saveDialog.FileName;
            var task = Task.Run(() => Download(downloadFileName, id));
            task.Wait();
        }

        static async Task Upload(string file, string content)
        {
            using (var mem = new MemoryStream(File.ReadAllBytes(content)))
            {
                await _dbx.Files.UploadAsync(
                    file,
                    WriteMode.Overwrite.Instance,
                    body: mem);
            }
        }

        public override void UploadFile(FileStructure curDir)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.FileName = "";
            if (openFileDialog.ShowDialog() == true)
            {
                string mimeType = "application/unknown";
                string ext = Path.GetExtension(openFileDialog.FileName).ToLower();
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                    mimeType = regKey.GetValue("Content Type").ToString();
                string fileName = openFileDialog.FileName;
                fileName = fileName.Substring(fileName.LastIndexOf('\\', fileName.Length - 2) + 1);
                fileName = curDir.Path + '/' + fileName;
                var task = Task.Run(() => Upload(fileName, openFileDialog.FileName)); ;
                task.Wait();
            }
        }

        public override void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir)
        {
            string path = curDir.Path;

            var list = _dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var item in cutFiles)
            {
                foreach (var listItem in list.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == item.Id) || (listItem.IsFolder && listItem.AsFolder.Id == item.Id))
                        _dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + listItem.Name);
                }
            }
        }

        public override void CreateFolder(string name, FileStructure parentDir)
        {
            string path = parentDir.Path;
            if (path == "/Dropbox")
            {
                _dbx.Files.CreateFolderV2Async("/" + name);
            }
            else
            {
                path = path.Substring(path.IndexOf("/") + 1);
                path = path.Substring(path.IndexOf("/"));
                _dbx.Files.CreateFolderV2Async(path + "/" + name);
            }
        }

        public override void RemoveFile(ICollection<FileStructure> selectedFiles)
        {
            var list = _dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var selectedItem in selectedFiles)
            {
                foreach (var listItem in list.Result.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == selectedItem.Id) || (listItem.IsFolder && listItem.AsFolder.Id == selectedItem.Id))
                        _dbx.Files.DeleteV2Async(listItem.PathDisplay);
                }
            }

        }

        public override void TrashFile(ICollection<FileStructure> selectedFiles)
        {
            RemoveFile(selectedFiles);
        }

        public override void UnTrashFile(ICollection<FileStructure> selectedFiles)
        {
            // There is no access to trash from .NET API
        }

        public override void ClearTrash()
        {
            // There is no access to trash from .NET API
        }

        public override void RenameFile(ICollection<FileStructure> selectedFiles, string newName)
        {
            var list = _dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var listItem in list.Entries)
            {
                if ((listItem.IsFile && listItem.AsFile.Id == selectedFiles.First<FileStructure>().Id)
                    || (listItem.IsFolder && listItem.AsFolder.Id == selectedFiles.First<FileStructure>().Id))
                {
                    var path = listItem.PathDisplay;
                    path = path.Substring(0, path.LastIndexOf("/"));
                    if (listItem.Name.IndexOf('.') >= 0)
                        newName += listItem.Name.Substring(listItem.Name.LastIndexOf('.'));
                    _dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + newName);
                }
            }
        }


        public override ObservableCollection<FileStructure> GetFiles()
        {
            var task = Task.Run(() => this.GetFolderFiles());
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
}
