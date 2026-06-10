using System;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Optional lease returned by <see cref="IRemoteControlProcessorFactory"/> implementations to release resources when a connection ends.
/// </summary>
public interface IRemoteControlProcessorLease : IAsyncDisposable, IDisposable
{
}