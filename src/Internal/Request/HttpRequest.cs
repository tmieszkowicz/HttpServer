using System.Text;
using Internal.Headers;

namespace Internal.Request;

public class RequestLine
{
    public string HttpVersion { get; set; } = string.Empty;
    public string RequestTarget { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

public enum ParserState
{
    Init,
    Headers,
    Body,
    Done
}

public class HttpRequest()
{
    public RequestLine? RequestLine { get; private set; }
    private ParserState _state = ParserState.Init;
    public HttpHeader Headers { get; private set; } = new();
    public string? Body { get; private set; } = string.Empty;

    public static HttpRequest? FromStream(Stream stream)
    {
        HttpRequest request = new();
        //could get overrun by a large request
        byte[] buffer = new byte[1024];
        int bufferLen = 0;

        while (!request.IsDone())
        {
            int bytesRead = stream.Read(buffer, bufferLen, buffer.Length - bufferLen);

            if (bytesRead == 0)
                throw new Exception("Stream ended");

            bufferLen += bytesRead;

            int consumed = request.Parse(buffer.AsSpan(0, bufferLen));

            if (consumed > 0)
            {
                Buffer.BlockCopy(buffer, consumed, buffer, 0, bufferLen - consumed);
                bufferLen -= consumed;
            }
        }

        return request;
    }


    private bool IsDone()
    {
        return _state == ParserState.Done;
    }

    private int Parse(ReadOnlySpan<byte> buffer)
    {
        int read = 0;

        while (buffer.Length > 0)
        {
            ReadOnlySpan<byte> currentData = buffer[read..];

            if (currentData.Length == 0)
                break;

            switch (_state)
            {
                case ParserState.Init:
                    (RequestLine? requestLine, int bytesRead) = ParseRequestLine(currentData);
                    if (bytesRead == 0)
                        return read;

                    RequestLine = requestLine;
                    read += bytesRead;
                    _state = ParserState.Headers;
                    break;

                case ParserState.Headers:
                    (int headersBytesRead, bool done) = Headers.Parse(currentData);
                    if (headersBytesRead < 0)
                        throw new Exception("Malformed header");
                    if (headersBytesRead == 0)
                        return read;

                    read += headersBytesRead;

                    if (done)
                    {
                        if (Headers.GetContentLength() != 0)
                        {
                            _state = ParserState.Body;
                        }
                        else
                        {
                            _state = ParserState.Done;
                        }
                    }

                    break;

                case ParserState.Body:
                    int length = Headers.GetContentLength();
                    if (length == 0)
                        throw new Exception("chunked encoding not supported");

                    int remaining = Math.Min(length - Body!.Length, currentData.Length);

                    Body += Encoding.UTF8.GetString(currentData[..remaining]);
                    read += remaining;

                    if (Body.Length == length)
                    {
                        _state = ParserState.Done;
                    }

                    break;

                case ParserState.Done:
                    return read;

                default:
                    throw new Exception($"Unknown state");
            }
        }

        return read;
    }



    public static (RequestLine? requestLine, int bytesRead) ParseRequestLine(ReadOnlySpan<byte> buffer)
    {
        ReadOnlySpan<byte> SEPARATOR = "\r\n"u8;

        int separatorIndex = buffer.IndexOf(SEPARATOR);

        if (separatorIndex == -1)
        {
            return (null, 0);
        }

        int bytesRead = separatorIndex + SEPARATOR.Length;

        int firstSlice = buffer[..separatorIndex].IndexOf((byte)' ');
        int secondSlice = buffer[(firstSlice + 1)..separatorIndex].IndexOf((byte)' ');

        if (firstSlice == -1 || secondSlice == -1)
            throw new Exception("Malformed request line");

        secondSlice += firstSlice + 1;

        ReadOnlySpan<byte> method = buffer[..firstSlice];
        ReadOnlySpan<byte> target = buffer[(firstSlice + 1)..secondSlice];
        ReadOnlySpan<byte> httpVersion = buffer[(secondSlice + 1)..separatorIndex];

        int slashIndex = httpVersion.IndexOf((byte)'/');
        ReadOnlySpan<byte> version = httpVersion[(slashIndex + 1)..];

        if (!version.SequenceEqual("1.1"u8))
            throw new Exception("HTTP version is not supported");

        RequestLine requestLine = new()
        {
            Method = Encoding.ASCII.GetString(method),
            RequestTarget = Encoding.ASCII.GetString(target),
            HttpVersion = Encoding.ASCII.GetString(version)
        };

        return (requestLine, bytesRead);
    }
}
