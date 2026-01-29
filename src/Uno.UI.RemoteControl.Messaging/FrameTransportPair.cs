using System;

namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// Holds a paired set of connected frame transports and manages their lifetime.
/// </summary>
public sealed class FrameTransportPair : IDisposable
{
	internal FrameTransportPair(IFrameTransport peer1, IFrameTransport peer2)
	{
		Peer1 = peer1 ?? throw new ArgumentNullException(nameof(peer1));
		Peer2 = peer2 ?? throw new ArgumentNullException(nameof(peer2));
	}

	/// <summary>
	/// Gets the first peer transport.
	/// </summary>
	public IFrameTransport Peer1 { get; }

	/// <summary>
	/// Gets the second peer transport.
	/// </summary>
	public IFrameTransport Peer2 { get; }

	/// <summary>
	/// Creates a pair of transports connected to each other in-memory.
	/// </summary>
	public static FrameTransportPair Create()
		=> InProcessFrameTransport.CreatePair();

	/// <summary>
	/// Deconstructs the pair into its peer transports.
	/// </summary>
	public void Deconstruct(out IFrameTransport peer1, out IFrameTransport peer2)
	{
		peer1 = Peer1;
		peer2 = Peer2;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		try
		{
			_ = Peer1.CloseAsync();
		}
		catch
		{
		}

		try
		{
			_ = Peer2.CloseAsync();
		}
		catch
		{
		}

		Peer1.Dispose();
		Peer2.Dispose();
	}
}
