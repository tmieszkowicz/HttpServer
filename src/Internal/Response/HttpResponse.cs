using System.Text;
using Internal.Headers;
using Internal.Request;

namespace Internal.Response;

public enum StatusCode
{
    StatusOk = 200,
    StatusBadRequest = 400,
    StatusInternalServerError = 500
}

public delegate void Handler(ResponseWriter writer, HttpRequest request);

public class ResponseWriter(Stream stream)
{
    private readonly Stream _stream = stream;

    public void WriteStatusLine(StatusCode statusCode)
    {
        HttpResponse.WriteStatusLine(_stream, statusCode);
    }

    public void WriteHeaders(HttpHeader headers)
    {
        HttpResponse.WriteHeaders(_stream, headers);
    }

    public void WriteBody(byte[] body)
    {
        _stream.Write(body, 0, body.Length);
    }
}

public class HttpResponse
{
    public static void WriteStatusLine(Stream writer, StatusCode statusCode)
    {
        byte[] statusLine = statusCode switch
        {
            StatusCode.StatusOk => Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n"),
            StatusCode.StatusBadRequest => Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\n"),
            StatusCode.StatusInternalServerError => Encoding.UTF8.GetBytes("HTTP/1.1 500 Internal Server Error\r\n"),
            _ => []
        };

        writer.Write(statusLine, 0, statusLine.Length);
    }

    public static HttpHeader GetDefaultHeaders(int contentLen)
    {
        HttpHeader headers = new();

        headers.Set("Content-Length", contentLen.ToString());
        headers.Set("Connection", "close");
        headers.Set("Content-Type", "text/plain");

        return headers;
    }

    public static void WriteHeaders(Stream writer, HttpHeader headers)
    {
        StringBuilder builder = new();
        foreach ((string? name, string? value) in headers.Headers)
        {
            builder.Append($"{name}: {value}\r\n");
        }
        builder.Append("\r\n");
        byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
        writer.Write(bytes, 0, bytes.Length);
    }
}
