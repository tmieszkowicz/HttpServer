using System.Text;
using Internal.Headers;
namespace Internal.Tests.Headers;

public class HttpHeadersTests
{
    [Fact]
    public void TestHeadersParse()
    {
        // Test: Valid single header
        byte[] data = Encoding.ASCII.GetBytes("Host: localhost:42069\r\nTest:      value\r\n\r\n");
        HttpHeader headers = new();
        (int bytesRead, bool done) parsed = headers.Parse(data);

        Assert.NotNull(headers);
        Assert.True(headers.Headers.ContainsKey("host"));
        Assert.Equal("localhost:42069", headers.Headers["host"]);
        Assert.True(headers.Headers.ContainsKey("test"));
        Assert.Equal("value", headers.Headers["test"]);
        Assert.Equal((43, true), parsed);

        // Test: Invalid spacing header
        data = Encoding.ASCII.GetBytes("       Host : localhost:42069       \r\n\r\n");
        headers = new HttpHeader();
        parsed = headers.Parse(data);
        Assert.Equal((-1, false), parsed);

        // Test: Invalid character in header name
        data = Encoding.ASCII.GetBytes("H@st: localhost:42069\r\n\r\n");
        headers = new HttpHeader();
        parsed = headers.Parse(data);
        Assert.Equal((-1, false), parsed);

        // Test: Duplicate headers
        data = Encoding.ASCII.GetBytes("Host: localhost:42069\r\nHost: localhost:42069\r\n");
        headers = new HttpHeader();
        parsed = headers.Parse(data);

        Assert.NotNull(headers);
        Assert.True(headers.Headers.ContainsKey("host"));
        Assert.Equal("localhost:42069, localhost:42069", headers.Headers["host"]);
        Assert.False(parsed.done);
    }
}
