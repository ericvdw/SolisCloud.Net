// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// A minimal loopback HTTP/1.1 server that captures the exact bytes of one request.
/// </summary>
/// <remarks>
/// In-memory handlers only see .NET's header objects. This server sees what the real HTTP stack put on the wire,
/// which is what the SolisCloud server verifies the signature against.
/// </remarks>
internal sealed class RawHttpServer : IDisposable
{
    private const string HeaderTerminator = "\r\n\r\n";

    private readonly TcpListener _listener;
    private readonly string _responseBody;

    public RawHttpServer(string responseBody)
    {
        _responseBody = responseBody;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        BaseAddress = new Uri("http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture) + "/");
    }

    public Uri BaseAddress { get; }

    /// <summary>
    /// Accepts one connection, returns the raw request text (headers and body), and sends the canned response.
    /// </summary>
    public async Task<string> CaptureOneRequestAsync(CancellationToken cancellationToken)
    {
#if NET
        using TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#else
        using TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
#endif
        using NetworkStream stream = client.GetStream();

        List<byte> received = [];
        byte[] buffer = new byte[4096];
        int headerEnd;
        while ((headerEnd = IndexOfTerminator(received)) < 0)
        {
            int read = await ReadAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new InvalidOperationException("Connection closed before the request headers were complete.");
            }

            received.AddRange(buffer.Take(read));
        }

        string head = Encoding.ASCII.GetString([.. received], 0, headerEnd);
        int contentLength = ParseContentLength(head);
        int expectedTotal = headerEnd + HeaderTerminator.Length + contentLength;
        while (received.Count < expectedTotal)
        {
            int read = await ReadAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            received.AddRange(buffer.Take(read));
        }

        byte[] body = Encoding.UTF8.GetBytes(_responseBody);
        string responseHead =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: application/json;charset=UTF-8\r\n" +
            "Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture) + "\r\n" +
            "Connection: close\r\n\r\n";
        byte[] headBytes = Encoding.ASCII.GetBytes(responseHead);
        await WriteAsync(stream, headBytes, cancellationToken).ConfigureAwait(false);
        await WriteAsync(stream, body, cancellationToken).ConfigureAwait(false);

        return Encoding.UTF8.GetString([.. received]);
    }

    public void Dispose()
    {
#if NET
        _listener.Dispose();
#else
        _listener.Stop();
#endif
    }

#if NET
    private static ValueTask<int> ReadAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.ReadAsync(buffer.AsMemory(), cancellationToken);

    private static ValueTask WriteAsync(NetworkStream stream, byte[] data, CancellationToken cancellationToken) =>
        stream.WriteAsync(data.AsMemory(), cancellationToken);
#else
    private static Task<int> ReadAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken) =>
        stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

    private static Task WriteAsync(NetworkStream stream, byte[] data, CancellationToken cancellationToken) =>
        stream.WriteAsync(data, 0, data.Length, cancellationToken);
#endif

    private static int IndexOfTerminator(List<byte> received)
    {
        string text = Encoding.ASCII.GetString([.. received]);
        return text.IndexOf(HeaderTerminator, StringComparison.Ordinal);
    }

    private static int ParseContentLength(string head)
    {
        foreach (string line in head.Split(["\r\n"], StringSplitOptions.None))
        {
            const string Prefix = "Content-Length:";
            if (line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(line.Substring(Prefix.Length).Trim(), NumberStyles.None, CultureInfo.InvariantCulture);
            }
        }

        return 0;
    }
}
