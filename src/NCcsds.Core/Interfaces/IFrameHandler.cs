namespace NCcsds.Core.Interfaces;

/// <summary>
/// Interface for handling frames in a processing pipeline.
/// </summary>
/// <typeparam name="TFrame">The frame type.</typeparam>
public interface IFrameHandler<TFrame>
{
    /// <summary>
    /// Handles a frame.
    /// </summary>
    /// <param name="frame">The frame to handle.</param>
    void Handle(TFrame frame);
}

/// <summary>
/// Interface for handling frames asynchronously.
/// </summary>
/// <typeparam name="TFrame">The frame type.</typeparam>
public interface IAsyncFrameHandler<TFrame>
{
    /// <summary>
    /// Handles a frame asynchronously.
    /// </summary>
    /// <param name="frame">The frame to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask HandleAsync(TFrame frame, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for frame sinks that receive and process frames.
/// </summary>
/// <typeparam name="TFrame">The frame type.</typeparam>
public interface IFrameSink<TFrame> : IFrameHandler<TFrame>, IDisposable
{
    /// <summary>
    /// Gets the number of frames received.
    /// </summary>
    long FramesReceived { get; }

    /// <summary>
    /// Gets the number of frames with errors.
    /// </summary>
    long FrameErrors { get; }
}

/// <summary>
/// Interface for frame sources that produce frames.
/// </summary>
/// <typeparam name="TFrame">The frame type.</typeparam>
public interface IFrameSource<TFrame>
{
    /// <summary>
    /// Gets an async enumerable of frames.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<TFrame> GetFramesAsync(CancellationToken cancellationToken = default);
}
