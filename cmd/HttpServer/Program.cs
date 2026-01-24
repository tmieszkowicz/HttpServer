using System.Security.Cryptography;
using System.Text;
using Internal.Headers;
using Internal.Request;
using Internal.Response;
using Internal.Server;

ushort port = 13000;
Server? server = null;

TaskCompletionSource shutdown = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.SetResult();
};

try
{
    server = Server.Serve(port, HandleRequest);
    Console.WriteLine($"Server started on port {port}");

    await shutdown.Task;
}
finally
{
    server?.Close();
}

void HandleRequest(ResponseWriter writer, HttpRequest request)
{
    HttpHeader headers = HttpResponse.GetDefaultHeaders(0);
    byte[] body = Respond200();
    StatusCode status = StatusCode.StatusOk;

    if (request.RequestLine!.RequestTarget == "/yourproblem")
    {
        body = Respond400();
        status = StatusCode.StatusBadRequest;
    }
    else if (request.RequestLine.RequestTarget == "/myproblem")
    {
        body = Respond500();
        status = StatusCode.StatusInternalServerError;
    }
    else if (request.RequestLine.RequestTarget == "/video")
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "video.mp4");
        if (File.Exists(path))
        {
            byte[] file = File.ReadAllBytes(path);
            headers.Replace("Content-Type", "video/mp4");
            headers.Replace("Content-Length", file.Length.ToString());
            writer.WriteStatusLine(StatusCode.StatusOk);
            writer.WriteHeaders(headers);
            writer.WriteBody(file);
            return;
        }
        else
        {
            body = Respond500();
            status = StatusCode.StatusInternalServerError;
        }
    }
    else if (request.RequestLine.RequestTarget.StartsWith("/httpbin/"))
    {
        string target = request.RequestLine.RequestTarget;
        string url = string.Concat("https://httpbin.org/", target.AsSpan("/httpbin/".Length));

        try
        {
            using HttpClient client = new();
            using HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
            using Stream stream = response.Content.ReadAsStreamAsync().Result;

            writer.WriteStatusLine(StatusCode.StatusOk);
            headers.Delete("Content-Length");
            headers.Set("Transfer-Encoding", "chunked");
            headers.Replace("Content-Type", "text/plain");
            headers.Set("Trailer", "X-Content-SHA256");
            headers.Set("Trailer", "X-Content-Length");
            writer.WriteHeaders(headers);

            List<byte> fullBody = [];
            byte[] buffer = new byte[32];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                byte[] chunk = buffer[..bytesRead];
                fullBody.AddRange(chunk);

                writer.WriteBody(Encoding.ASCII.GetBytes($"{bytesRead:x}\r\n"));
                writer.WriteBody(chunk);
                writer.WriteBody(Encoding.ASCII.GetBytes("\r\n"));
            }

            writer.WriteBody(Encoding.ASCII.GetBytes("0\r\n"));

            HttpHeader trailer = new();
            byte[] hash = SHA256.HashData([.. fullBody]);
            trailer.Set("X-Content-SHA256", Convert.ToHexString(hash));
            trailer.Set("X-Content-Length", fullBody.Count.ToString());
            writer.WriteHeaders(trailer);
            return;
        }
        catch
        {
            body = Respond500();
            status = StatusCode.StatusInternalServerError;
        }
    }

    headers.Replace("Content-Length", body.Length.ToString());
    headers.Replace("Content-Type", "text/html");
    writer.WriteStatusLine(status);
    writer.WriteHeaders(headers);
    writer.WriteBody(body);
}

byte[] Respond200() => Encoding.UTF8.GetBytes(@"<html>
  <head>
    <title>200 OK</title>
  </head>
  <body>
    <h1>Success</h1>
    <p>Your request is a banger.</p>
  </body>
</html>");

byte[] Respond400() => Encoding.UTF8.GetBytes(@"<html>
  <head>
    <title>400 Bad Request</title>
  </head>
  <body>
    <h1>Bad Request</h1>
    <p>Your request stinks.</p>
  </body>
</html>");

byte[] Respond500() => Encoding.UTF8.GetBytes(@"<html>
  <head>
    <title>500 Internal Server Error</title>
  </head>
  <body>
    <h1>Internal Server Error</h1>
    <p>This one's on me, your majesty.</p>
  </body>
</html>");
