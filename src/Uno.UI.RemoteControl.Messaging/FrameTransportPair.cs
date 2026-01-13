namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// Factory for creating paired in-process frame transports.
/// </summary>
public static class FrameTransportPair
{
	/// <summary>
	/// Creates a pair of transports connected to each other in-memory.
	/// </summary>
	/// <returns>
	/// A tuple containing interconnected transports.
	/// </returns>
	public static (IFrameTransport Peer1, IFrameTransport Peer2) Create()
		=> InProcessFrameTransport.CreatePair();
}
