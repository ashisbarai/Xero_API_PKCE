using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Xero_API_PKCE.WebServers
{
    public static class WebServerListener
    {
        private static readonly HttpListener HttpListener = new() { Prefixes = { "http://localhost:8800/" } };

        private static bool _serverStarted = true;

        private static Task? _listenerTask;

        public static void StartWebServer(Action<string> handleCode)
        {
            if (_listenerTask is { IsCompleted: false }) return;
            {
                _listenerTask = ManageHttpListener(handleCode);
            }
        }

        public static void StopWebServer()
        {
            _serverStarted = false;
            lock (HttpListener)
            {
                HttpListener.Stop();
            }
            try
            {
                _listenerTask?.Wait();
            }
            catch
            {
                // ignored
            }
        }

        private static async Task ManageHttpListener(Action<string> handleCode)
        {
            HttpListener.Start();

            while (_serverStarted)
            {
                try
                {
                    var context = await HttpListener.GetContextAsync();
                    lock (HttpListener)
                    {
                        if (_serverStarted) ProcessHttpRequest(context, handleCode);
                    }
                }
                catch (HttpListenerException)
                {
                    return;
                }
            }
        }

        private static void ProcessHttpRequest(HttpListenerContext context, Action<string> handleCode)
        {
            using var response = context.Response;
            try
            {
                var handled = false;
                switch (context.Request.Url?.AbsolutePath)
                {
                    case "/callback":
                        handled = HandleCallbackRequest(context, response, handleCode);
                        break;
                }
                if (!handled)
                {
                    response.StatusCode = 404;
                }
            }
            catch (Exception e)
            {
                response.StatusCode = 500;
                response.ContentType = "application/json";

                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e));
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        private static bool HandleCallbackRequest(HttpListenerContext context, HttpListenerResponse response, Action<string> handleCode)
        {
            string? GetParamValue(IEnumerable<string> queryParams, string name)
            {
                return queryParams.Select(p => p.Split("=")).Where(kv => kv.First() == name).Select(kv => kv.Last())
                    .FirstOrDefault();
            }
            var query = context.Request.Url?.Query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Split("?").Last();
                var parts = query.Split("&");
                var code = GetParamValue(parts, "code");
                if (!string.IsNullOrWhiteSpace(code))
                {
                    handleCode(code);
                }
            }
            var buffer = "Successfully Authenticated!"u8.ToArray();
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);

            return true;
        }
    }
}