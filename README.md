# C# HTTP Server

A minimal HTTP/1.1 server written in C#, inspired by Learn the HTTP Protocol from boot.dev

Runs on port 13000 by default

## Endpoints

### `/`
Returns **200 OK**
### `/yourproblem`
Returns **400 Bad Request**
### `/myproblem`
Returns **500 Internal Server Error**
### `/video`
Streams `video.mp4` if present
### `/httpbin/*`
Proxies requests to `https://httpbin.org/*` and streams the response back using chunked encoding

## Example usage
```bash
curl -v http://localhost:13000/yourproblem
```
```bash
curl -v http://localhost:13000/video -o video.mp4
```
```bash
echo "GET /httpbin/stream/100 HTTP/1.1`r`nHost: localhost:13000`r`nConnection: close`r`n`r`n" | ncat localhost 13000
```
