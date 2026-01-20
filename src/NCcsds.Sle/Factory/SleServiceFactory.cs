using NCcsds.Sle.Cltu;
using NCcsds.Sle.Common;
using NCcsds.Sle.Raf;
using NCcsds.Sle.Rcf;
using NCcsds.Sle.Rocf;
using NCcsds.Sle.Transport;

namespace NCcsds.Sle.Factory;

/// <summary>
/// Factory for creating SLE service instances.
/// </summary>
public class SleServiceFactory
{
    /// <summary>
    /// Creates a RAF service instance.
    /// </summary>
    public RafServiceInstance CreateRafService(SleServiceConfiguration config)
    {
        var service = new RafServiceInstance
        {
            ServiceInstanceId = config.ServiceInstanceId,
            Version = config.Version,
            Credentials = config.Credentials
        };
        return service;
    }

    /// <summary>
    /// Creates a RCF service instance.
    /// </summary>
    public RcfServiceInstance CreateRcfService(SleServiceConfiguration config)
    {
        var service = new RcfServiceInstance
        {
            ServiceInstanceId = config.ServiceInstanceId,
            Version = config.Version,
            Credentials = config.Credentials
        };
        return service;
    }

    /// <summary>
    /// Creates a ROCF service instance.
    /// </summary>
    public RocfServiceInstance CreateRocfService(SleServiceConfiguration config)
    {
        var service = new RocfServiceInstance
        {
            ServiceInstanceId = config.ServiceInstanceId,
            Version = config.Version,
            Credentials = config.Credentials
        };
        return service;
    }

    /// <summary>
    /// Creates a CLTU service instance.
    /// </summary>
    public CltuServiceInstance CreateCltuService(SleServiceConfiguration config)
    {
        var service = new CltuServiceInstance
        {
            ServiceInstanceId = config.ServiceInstanceId,
            Version = config.Version,
            Credentials = config.Credentials
        };
        return service;
    }

    /// <summary>
    /// Creates a transport for the given configuration.
    /// </summary>
    public ISleTransport CreateTransport(SleTransportConfiguration config)
    {
        return new SleTcpTransport(
            config.Host,
            config.Port,
            config.UseTls,
            config.ClientCertificate);
    }
}

/// <summary>
/// SLE service configuration.
/// </summary>
public class SleServiceConfiguration
{
    /// <summary>
    /// Service instance identifier (SIID).
    /// </summary>
    public string ServiceInstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Service type.
    /// </summary>
    public SleServiceType ServiceType { get; set; }

    /// <summary>
    /// SLE version to use.
    /// </summary>
    public SleVersion Version { get; set; } = SleVersion.V5;

    /// <summary>
    /// Authentication credentials.
    /// </summary>
    public SleCredentials? Credentials { get; set; }

    /// <summary>
    /// Initiator identifier.
    /// </summary>
    public string InitiatorId { get; set; } = string.Empty;

    /// <summary>
    /// Responder identifier.
    /// </summary>
    public string ResponderId { get; set; } = string.Empty;

    /// <summary>
    /// Transport configuration.
    /// </summary>
    public SleTransportConfiguration Transport { get; set; } = new();
}

/// <summary>
/// Managed SLE service that combines service instance with transport.
/// </summary>
public class ManagedSleService<TService> : IAsyncDisposable where TService : class
{
    private readonly TService _service;
    private readonly ISleTransport _transport;
    private readonly SleTransportConfiguration _transportConfig;
    private int _reconnectAttempts;

    /// <summary>
    /// The underlying service instance.
    /// </summary>
    public TService Service => _service;

    /// <summary>
    /// The transport layer.
    /// </summary>
    public ISleTransport Transport => _transport;

    /// <summary>
    /// Creates a managed SLE service.
    /// </summary>
    public ManagedSleService(TService service, ISleTransport transport, SleTransportConfiguration transportConfig)
    {
        _service = service;
        _transport = transport;
        _transportConfig = transportConfig;

        _transport.ConnectionLost += OnConnectionLost;
    }

    /// <summary>
    /// Connects to the service provider.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _transport.ConnectAsync(cancellationToken);
        _reconnectAttempts = 0;
    }

    /// <summary>
    /// Disconnects from the service provider.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _transport.DisconnectAsync(cancellationToken);
    }

    private async void OnConnectionLost(Exception? ex)
    {
        if (!_transportConfig.AutoReconnect)
            return;

        if (_transportConfig.MaxReconnectAttempts > 0 &&
            _reconnectAttempts >= _transportConfig.MaxReconnectAttempts)
            return;

        _reconnectAttempts++;

        try
        {
            await Task.Delay(_transportConfig.ReconnectDelay);
            await _transport.ConnectAsync();
        }
        catch
        {
            // Reconnect failed, will try again on next connection lost event
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _transport.ConnectionLost -= OnConnectionLost;
        await _transport.DisconnectAsync();
        _transport.Dispose();
    }
}
