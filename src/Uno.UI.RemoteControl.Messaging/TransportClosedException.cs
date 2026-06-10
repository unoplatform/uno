using System;

namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// Exception raised by a transport implementation to signal closure in a transport-agnostic way.
/// </summary>
public sealed class TransportClosedException : Exception
{
	public TransportClosedException()
	{
	}

	public TransportClosedException(string message)
		: base(message)
	{
	}

	public TransportClosedException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
