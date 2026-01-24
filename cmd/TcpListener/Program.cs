using System.Net;
using System.Net.Sockets;
using Internal.Request;

TcpListener listener = new(IPAddress.Any, 13000);


//echo "GET / HTTP/1.1`r`nHost: localhost:42069`r`nUser-Agent: curl/7.81.0`r`nAccept: */*`r`n`r`n" | ncat 127.0.0.1 13000
try
{
    listener.Start();

    while (true)
    {

        TcpClient client = listener.AcceptTcpClient();
        Stream stream = client.GetStream();

        HttpRequest? request = HttpRequest.FromStream(stream);

        if (request?.RequestLine is null)
        {
            client.Close();
            continue;
        }

        Console.WriteLine("Request line:");
        Console.WriteLine($"- Method: {request.RequestLine.Method}");
        Console.WriteLine($"- Target: {request.RequestLine.RequestTarget}");
        Console.WriteLine($"- Version: {request.RequestLine.HttpVersion}");

        Console.WriteLine("Headers:");
        foreach ((string? name, string? value) in request.Headers.Headers)
        {
            Console.WriteLine($"- {name}: {value}");
        }

        if (request.Headers.Headers.TryGetValue("body", out string? body))
        {
            Console.WriteLine("Body:");
            Console.WriteLine($"{body}");
        }

        client.Close();
    }

}
catch (SocketException e)
{
    Console.WriteLine($"Socket exception: {e}");
}
finally
{
    listener.Stop();
}
