using System.Net;
using System.Net.Sockets;
using Internal.Request;
using Internal.Response;

namespace Internal.Server;

public class Server
{
    private bool _isClosed;
    private readonly TcpListener _listener = null!;
    private Handler _handler = null!;

    private Server(Handler handler)
    {
        _isClosed = false;
        _handler = handler;
    }

    private static void RunConnection(Server server, Stream stream)
    {
        try
        {
            ResponseWriter writer = new(stream);
            HttpRequest request;

            try
            {
                request = HttpRequest.FromStream(stream)!;
            }
            catch
            {
                writer.WriteStatusLine(StatusCode.StatusBadRequest);
                writer.WriteHeaders(HttpResponse.GetDefaultHeaders(0));
                return;
            }

            server._handler(writer, request);
        }
        finally
        {
            stream.Close();
        }
    }

    private static void RunServer(Server server, TcpListener listener)
    {
        while (true)
        {
            TcpClient conn;

            try
            {
                conn = listener.AcceptTcpClient();
            }
            catch
            {
                if (server._isClosed) return;
                continue;
            }

            if (server._isClosed) return;

            Stream stream = conn.GetStream();
            Task.Run(() => RunConnection(server, stream));
        }
    }

    public static Server Serve(ushort port, Handler handler)
    {
        TcpListener listener = new(IPAddress.Any, port);
        listener.Start();

        Server server = new(handler)
        {
            _handler = handler,
        };

        Task.Run(() => RunServer(server, listener));
        return server;
    }

    public void Close()
    {
        _isClosed = true;
        _listener?.Stop();
    }
}
