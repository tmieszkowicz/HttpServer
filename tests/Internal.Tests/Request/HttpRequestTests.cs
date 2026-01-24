using System.Text;
using Internal.Request;

namespace Internal.Tests.Request;

public class HttpRequestTests
{
    [Fact]
    public void TestParseRequestLine()
    {
        ChunkReader reader = new(
            data: Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n"),
            numBytesPerRead: 3);

        var request = HttpRequest.FromStream(reader);

        Assert.NotNull(request);
        Assert.Equal("GET", request.RequestLine?.Method);
        Assert.Equal("/", request.RequestLine?.RequestTarget);
        Assert.Equal("1.1", request.RequestLine?.HttpVersion);
    }

    [Fact]
    public void TestParseHeaders()
    {
        ChunkReader reader = new(
            data: Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n"),
            numBytesPerRead: 3);

        var request = HttpRequest.FromStream(reader);

        Assert.NotNull(request);
        Assert.True(request.Headers.Headers.TryGetValue("host", out string? host));
        Assert.Equal("localhost:42069", host);
        Assert.True(request.Headers.Headers.TryGetValue("user-agent", out string? userAgent));
        Assert.Equal("curl/7.81.0", userAgent);
        Assert.True(request.Headers.Headers.TryGetValue("accept", out string? accept));
        Assert.Equal("*/*", accept);

        reader = new(
            data: Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost localhost:42069\r\n\r\n"),
            numBytesPerRead: 3);

        Assert.Throws<Exception>(() => HttpRequest.FromStream(reader));
    }


    [Fact]
    public void TestParseBody()
    {
        ChunkReader reader = new(
            data: Encoding.ASCII.GetBytes("POST /submit HTTP/1.1\r\nHost: localhost:42069\r\nContent-Length: 12\r\n\r\nhello world!"),
            numBytesPerRead: 3);

        var request = HttpRequest.FromStream(reader);

        Assert.NotNull(request);
        Assert.Equal("hello world!", request.Body);

        reader = new(
            data: Encoding.ASCII.GetBytes("POST /submit HTTP/1.1\r\nHost: localhost:42069\r\nContent-Length: 20\r\n\r\npartial content"),
            numBytesPerRead: 3);

        Assert.Throws<Exception>(() => HttpRequest.FromStream(reader));
    }

}

sealed class ChunkReader(byte[] data, int numBytesPerRead) : Stream
{
    readonly int numBytesPerRead = numBytesPerRead;
    int pos;

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (pos >= data.Length)
            return 0; // EOF

        int n = Math.Min(numBytesPerRead, data.Length - pos);
        Array.Copy(data, pos, buffer, offset, n);
        pos += n;
        return n;
    }

    public override bool CanRead => throw new NotImplementedException();

    public override bool CanSeek => throw new NotImplementedException();

    public override bool CanWrite => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}
