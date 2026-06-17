#nullable enable

using System;
using System.Threading;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// Static accessor to the process-wide tool &amp; resource registry singleton. Exposes two
/// segregated faces: <see cref="Publisher"/> (registration, for publishers) and <see cref="Catalog"/>
/// (consumption, for consumers). The registry has no transport dependency; a consumer reads it
/// in-process and owns any messaging.
/// </summary>
internal static class ToolRegistry
{
	private static IToolRegistry _instance = new ToolRegistryImpl();

	/// <summary>The registration face — used by publishers.</summary>
	public static IToolPublisher Publisher => _instance;

	/// <summary>The consumption face — used by consumers.</summary>
	public static IToolCatalog Catalog => _instance;

	/// <summary>
	/// Wires the UI-thread dispatcher used to marshal tool invocations declared with
	/// <c>runOnUIThread: true</c>. Supplied by the host (the Remote Control client). When unset,
	/// handlers run inline on the caller's thread.
	/// </summary>
	internal static void SetDispatcher(IToolDispatcher? dispatcher)
	{
		if (_instance is ToolRegistryImpl impl)
		{
			impl.Dispatcher = dispatcher;
		}
	}

	/// <summary>Swaps the backing registry for tests; dispose the result to restore the previous one.</summary>
	internal static IDisposable SetForTesting(IToolRegistry instance)
		=> new RestoreToken(Interlocked.Exchange(ref _instance, instance));

	private sealed class RestoreToken(IToolRegistry previous) : IDisposable
	{
		private IToolRegistry? _previous = previous;

		public void Dispose()
		{
			// Restore the field only; the caller owns disposal of the swapped-in double.
			if (Interlocked.Exchange(ref _previous, null) is { } previous)
			{
				_instance = previous;
			}
		}
	}
}
