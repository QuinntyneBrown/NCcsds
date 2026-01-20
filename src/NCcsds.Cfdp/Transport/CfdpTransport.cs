using System.Net;
using System.Net.Sockets;

namespace NCcsds.Cfdp.Transport;

/// <summary>
/// CFDP transport layer abstraction.
/// </summary>
public interface ICfdpTransport : IDisposable
{
    /// <summary>
    /// Sends a PDU to a destination entity.
    /// </summary>
    Task SendAsync(byte[] pdu, ulong destinationEntityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a PDU is received.
    /// </summary>
    event Action<byte[]>? PduReceived;

    /// <summary>
    /// Starts the transport.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the transport.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// UDP transport for CFDP.
/// </summary>
public class CfdpUdpTransport : ICfdpTransport
{
    private readonly CfdpTransportConfiguration _config;
    private UdpClient? _client;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    /// <inheritdoc />
    public event Action<byte[]>? PduReceived;

    /// <summary>
    /// Creates a new UDP transport.
    /// </summary>
    public CfdpUdpTransport(CfdpTransportConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _client = new UdpClient(_config.LocalPort);
        _receiveCts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _receiveCts?.Cancel();
        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch { /* Ignore cancellation */ }
        }
        _client?.Dispose();
        _client = null;
    }

    /// <inheritdoc />
    public async Task SendAsync(byte[] pdu, ulong destinationEntityId, CancellationToken cancellationToken = default)
    {
        if (_client == null)
            throw new InvalidOperationException("Transport not started");

        if (!_config.EntityEndpoints.TryGetValue(destinationEntityId, out var endpoint))
            throw new InvalidOperationException($"No endpoint configured for entity {destinationEntityId}");

        await _client.SendAsync(pdu, pdu.Length, endpoint);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _client != null)
        {
            try
            {
                var result = await _client.ReceiveAsync(cancellationToken);
                PduReceived?.Invoke(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Log and continue
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _receiveCts?.Cancel();
        _client?.Dispose();
    }
}

/// <summary>
/// TCP transport for CFDP.
/// </summary>
public class CfdpTcpTransport : ICfdpTransport
{
    private readonly CfdpTransportConfiguration _config;
    private TcpListener? _listener;
    private readonly Dictionary<ulong, TcpClient> _connections = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _listenerCts;
    private Task? _listenerTask;

    /// <inheritdoc />
    public event Action<byte[]>? PduReceived;

    /// <summary>
    /// Creates a new TCP transport.
    /// </summary>
    public CfdpTcpTransport(CfdpTransportConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Any, _config.LocalPort);
        _listener.Start();
        _listenerCts = new CancellationTokenSource();
        _listenerTask = AcceptLoopAsync(_listenerCts.Token);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _listenerCts?.Cancel();
        _listener?.Stop();

        if (_listenerTask != null)
        {
            try { await _listenerTask; } catch { /* Ignore cancellation */ }
        }

        lock (_lock)
        {
            foreach (var conn in _connections.Values)
                conn.Dispose();
            _connections.Clear();
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(byte[] pdu, ulong destinationEntityId, CancellationToken cancellationToken = default)
    {
        TcpClient? client;

        lock (_lock)
        {
            if (!_connections.TryGetValue(destinationEntityId, out client) || !client.Connected)
            {
                if (!_config.EntityEndpoints.TryGetValue(destinationEntityId, out var endpoint))
                    throw new InvalidOperationException($"No endpoint configured for entity {destinationEntityId}");

                client = new TcpClient();
                client.Connect(endpoint);
                _connections[destinationEntityId] = client;

                // Start receiving from this connection
                _ = ReceiveLoopAsync(client, CancellationToken.None);
            }
        }

        var stream = client.GetStream();

        // Frame with length prefix (4 bytes)
        var frame = new byte[4 + pdu.Length];
        frame[0] = (byte)(pdu.Length >> 24);
        frame[1] = (byte)(pdu.Length >> 16);
        frame[2] = (byte)(pdu.Length >> 8);
        frame[3] = (byte)pdu.Length;
        pdu.CopyTo(frame, 4);

        await stream.WriteAsync(frame, cancellationToken);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = ReceiveLoopAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Log and continue
            }
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var stream = client.GetStream();
            var lengthBuffer = new byte[4];

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                // Read length prefix
                var read = await ReadExactlyAsync(stream, lengthBuffer, cancellationToken);
                if (read < 4) break;

                var length = (lengthBuffer[0] << 24) | (lengthBuffer[1] << 16) |
                            (lengthBuffer[2] << 8) | lengthBuffer[3];

                // Read PDU
                var pdu = new byte[length];
                read = await ReadExactlyAsync(stream, pdu, cancellationToken);
                if (read < length) break;

                PduReceived?.Invoke(pdu);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch
        {
            // Connection closed or error
        }
        finally
        {
            client.Dispose();
        }
    }

    private static async Task<int> ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read == 0) return offset;
            offset += read;
        }
        return offset;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listenerCts?.Cancel();
        _listener?.Stop();

        lock (_lock)
        {
            foreach (var conn in _connections.Values)
                conn.Dispose();
            _connections.Clear();
        }
    }
}

/// <summary>
/// CFDP transport configuration.
/// </summary>
public class CfdpTransportConfiguration
{
    /// <summary>
    /// Local port to listen on.
    /// </summary>
    public int LocalPort { get; set; } = 1234;

    /// <summary>
    /// Mapping of entity IDs to endpoints.
    /// </summary>
    public Dictionary<ulong, IPEndPoint> EntityEndpoints { get; set; } = new();
}
