#nullable enable

using System;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// Optional seam letting the registry marshal tool invocations onto the UI thread. Supplied by the
/// host (the Remote Control client) which knows the dispatcher; when absent, handlers run inline.
/// </summary>
internal interface IToolDispatcher
{
	/// <summary>Whether the current thread is the UI thread (run inline when true to avoid a re-dispatch/deadlock).</summary>
	bool HasThreadAccess { get; }

	/// <summary>Runs <paramref name="action"/> on the UI thread and returns its result.</summary>
	Task<T> RunAsync<T>(Func<Task<T>> action);
}
