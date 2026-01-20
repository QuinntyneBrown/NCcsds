using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NCcsds.Sle.Transport;

/// <summary>
/// SLE transport layer abstraction.
/// </summary>
public interface ISleTransport : IDisposable
{
    /// <summary>
    /// Whether the transport is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the remote endpoint.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the remote endpoint.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PDU.
    /// </summary>
    Task SendAsync(byte[] pdu, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a PDU.
    /// </summary>
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when data is received.
    /// </summary>
    event Action<byte[]>? DataReceived;

    /// <summary>
    /// Event raised when the connection is lost.
    /// </summary>
    event Action<Exception?>? ConnectionLost;
}

/// <summary>
/// SLE TCP transport implementation.
/// </summary>
public class SleTcpTransport : ISleTransport
{
    private TcpClient? _client;
    private Stream? _stream;
    private readonly string _host;
    private readonly int _port;
    private readonly bool _useTls;
    private readonly X509Certificate2? _clientCertificate;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    /// <summary>
    /// Creates a new SLE TCP transport.
    /// </summary>
    public SleTcpTransport(string host, int port, bool useTls = false, X509Certificate2? clientCertificate = null)
    {
        _host = host;
        _port = port;
        _useTls = useTls;
        _clientCertificate = clientCertificate;
    }

    /// <inheritdoc />
    public bool IsConnected => _client?.Connected ?? false;

    /// <inheritdoc />
    public event Action<byte[]>? DataReceived;

    /// <inheritdoc />
    public event Action<Exception?>? ConnectionLost;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_host, _port, cancellationToken);

        if (_useTls)
        {
            var sslStream = new SslStream(_client.GetStream(), false, ValidateServerCertificate);
            var clientCerts = new X509CertificateCollection();
            if (_clientCertificate != null)
                clientCerts.Add(_clientCertificate);

            await sslStream.AuthenticateAsClientAsync(_host);
            _stream = sslStream;
        }
        else
        {
            _stream = _client.GetStream();
        }

        _receiveCts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _receiveCts?.Cancel();
        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch { /* Ignore cancellation */ }
        }

        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    /// <inheritdoc />
    public async Task SendAsync(byte[] pdu, CancellationToken cancellationToken = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Transport not connected");

        // SLE uses TML (Transport Mapping Layer) with length prefix
        var lengthPrefix = new byte[8];
        // TML version (1) + reserved (3) + length (4)
        lengthPrefix[0] = 0x01; // Version 1
        var length = pdu.Length;
        lengthPrefix[4] = (byte)(length >> 24);
        lengthPrefix[5] = (byte)(length >> 16);
        lengthPrefix[6] = (byte)(length >> 8);
        lengthPrefix[7] = (byte)length;

        await _stream.WriteAsync(lengthPrefix, cancellationToken);
        await _stream.WriteAsync(pdu, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Transport not connected");

        // Read TML header (8 bytes)
        var header = new byte[8];
        await ReadExactlyAsync(_stream, header, cancellationToken);

        // Extract length
        var length = (header[4] << 24) | (header[5] << 16) | (header[6] << 8) | header[7];

        // Read PDU
        var pdu = new byte[length];
        await ReadExactlyAsync(_stream, pdu, cancellationToken);

        return pdu;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var pdu = await ReceiveAsync(cancellationToken);
                DataReceived?.Invoke(pdu);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            ConnectionLost?.Invoke(ex);
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("Connection closed");
            offset += read;
        }
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        // In production, implement proper certificate validation
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _receiveCts?.Cancel();
        _stream?.Dispose();
        _client?.Dispose();
    }
}

/// <summary>
/// SLE transport configuration.
/// </summary>
public class SleTransportConfiguration
{
    /// <summary>
    /// Remote host address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Remote port number.
    /// </summary>
    public int Port { get; set; } = 5100;

    /// <summary>
    /// Whether to use TLS.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Client certificate for TLS authentication.
    /// </summary>
    public X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Connection timeout.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Heartbeat interval (0 to disable).
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(25);

    /// <summary>
    /// Heartbeat timeout.
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to auto-reconnect on connection loss.
    /// </summary>
    public bool AutoReconnect { get; set; }

    /// <summary>
    /// Reconnection delay.
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum reconnection attempts (0 = unlimited).
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 3;
}
