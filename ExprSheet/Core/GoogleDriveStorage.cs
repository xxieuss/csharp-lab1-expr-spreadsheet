using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Core;

public class GoogleDriveStorage : IStorage {
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };

    private readonly string _clientSecretJson;
    private readonly string _tokenFolder;
    private readonly Action<string> _openBrowser;
    private DriveService? _service;

    public string AccessToken { get; private set; } = "";

    public GoogleDriveStorage(string clientSecretJson, string tokenFolder, Action<string> openBrowser) {
        _clientSecretJson = clientSecretJson;
        _tokenFolder = tokenFolder;
        _openBrowser = openBrowser;
    }

    public void Authorize() {
        if (_service != null) return;

        try {
            using MemoryStream secrets = new MemoryStream(Encoding.UTF8.GetBytes(_clientSecretJson));

            GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer {
                    ClientSecrets = GoogleClientSecrets.FromStream(secrets).Secrets,
                    Scopes = Scopes,
                    DataStore = new FileDataStore(_tokenFolder, true)
                });

            UserCredential credential = new AuthorizationCodeInstalledApp(flow, new LoopbackCodeReceiver(_openBrowser))
                .AuthorizeAsync("user", CancellationToken.None).GetAwaiter().GetResult();

            AccessToken = credential.Token.AccessToken ?? "";

            _service = new DriveService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                ApplicationName = "ExprSheet"
            });
        } catch (StorageException) {
            throw;
        } catch (Exception ex) {
            throw new StorageException($"Не вдалося авторизуватися в Google Drive: {ex.Message}");
        }
    }

    public void Save(Sheet sheet, string name) {
        Authorize();

        try {
            string? id = FindFileId(name);
            using MemoryStream content = new MemoryStream(Encoding.UTF8.GetBytes(sheet.ToJson()));

            IUploadProgress progress;

            if (id == null) {
                progress = _service!.Files.Create(new DriveFile { Name = name }, content, "application/json").Upload();
            } else {
                progress = _service!.Files.Update(new DriveFile(), id, content, "application/json").Upload();
            }

            if (progress.Status != UploadStatus.Completed)
                throw new StorageException($"Не вдалося передати файл: {progress.Exception?.Message}");

        } catch (StorageException) {
            throw;
        } catch (Exception ex) {
            throw new StorageException($"Помилка Google Drive: {ex.Message}");
        }
    }

    public Sheet Load(string name) {
        Authorize();

        try {
            string? id = FindFileId(name);
            if (id == null) throw new StorageException($"На Google Drive немає файлу «{name}»");

            using MemoryStream stream = new MemoryStream();
            _service!.Files.Get(id).Download(stream);

            return Sheet.FromJson(Encoding.UTF8.GetString(stream.ToArray()));

        } catch (StorageException) {
            throw;
        } catch (Exception ex) {
            throw new StorageException($"Помилка Google Drive: {ex.Message}");
        }
    }

    private string? FindFileId(string name) {
        FilesResource.ListRequest request = _service!.Files.List();
        request.Q = $"name = '{name.Replace("'", "\\'")}' and trashed = false";
        request.Fields = "files(id, name)";
        request.PageSize = 10;

        FileList result = request.Execute();
        return result.Files.Count > 0 ? result.Files[0].Id : null;
    }
}

internal sealed class LoopbackCodeReceiver : ICodeReceiver {
    private const string ClosePage =
        "<html><head><meta charset=\"utf-8\"><title>ExprSheet</title></head>" +
        "<body style=\"font-family:system-ui;padding:40px\"><h3>Дозвіл отримано</h3>" +
        "<p>Можна закрити це вікно й повернутися до програми.</p></body></html>";

    private readonly Action<string> _openBrowser;
    private readonly TcpListener _listener;

    public string RedirectUri { get; }

    public LoopbackCodeReceiver(Action<string> openBrowser) {
        _openBrowser = openBrowser;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        RedirectUri = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/authorize/";
    }

    public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
            AuthorizationCodeRequestUrl url, CancellationToken token) {
        try {
            _openBrowser(url.Build().ToString());
            return Parse(await ReadRequestAsync(token));
        } finally {
            _listener.Stop();
        }
    }

    private async Task<string> ReadRequestAsync(CancellationToken token) {
        using TcpClient client = await _listener.AcceptTcpClientAsync(token);
        using NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[8192];
        int read = await stream.ReadAsync(buffer, token);
        if (read == 0) throw new StorageException("Google не передав код авторизації");

        byte[] body = Encoding.UTF8.GetBytes(ClosePage);
        byte[] head = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
                                             $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head, token);
        await stream.WriteAsync(body, token);
        await stream.FlushAsync(token);

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static AuthorizationCodeResponseUrl Parse(string request) {
        int start = request.IndexOf('?');
        int end = request.IndexOf(' ', start + 1);
        if (start < 0 || end < 0) throw new StorageException("Google не передав код авторизації");

        AuthorizationCodeResponseUrl result = new AuthorizationCodeResponseUrl();

        foreach (string pair in request.Substring(start + 1, end - start - 1).Split('&')) {
            int eq = pair.IndexOf('=');
            if (eq < 0) continue;

            string key = pair.Substring(0, eq);
            string value = Uri.UnescapeDataString(pair.Substring(eq + 1));

            if (key == "code") result.Code = value;
            else if (key == "state") result.State = value;
            else if (key == "error") result.Error = value;
        }

        if (result.Error != null) throw new StorageException($"Google відмовив у доступі: {result.Error}");
        return result;
    }
}