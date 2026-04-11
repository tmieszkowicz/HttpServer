# C# HTTP Server

A minimal HTTP/1.1 server written in C#, inspired by [Learn the HTTP Protocol in Go](https://www.boot.dev/courses/learn-http-protocol-golang) from boot.dev. Runs on port 13000 by default.

## Usage

```bash
dotnet build
dotnet run --project cmd/HttpServer/HttpServer.csproj
```

## Endpoints

| Path         | Description                                                   |
| ------------ | ------------------------------------------------------------- |
| /            | 200 OK                                                        |
| /yourproblem | 400 Bad Request                                               |
| /myproblem   | 500 Internal Server Error                                     |
| /video       | Streams video.mp4 if present                                  |
| /httpbin/\*  | Proxies to httpbin.org, streams response via chunked encoding |

## Examples

**Basic endpoint**

```bash
curl http://localhost:13000/yourproblem
```

**Save video to current directory**

```bash
curl http://localhost:13000/video -o video.mp4
```

**Stream JSON responses**

```bash
curl http://localhost:13000/httpbin/stream/100
```
