#nullable enable

using System.Threading;
using Uno.ApplicationModel.Core;
using Uno.Foundation.Logging;

namespace Uno.Extensions.ApplicationModel.Core;

internal class CoreApplicationExtension : ICoreApplicationExtension
{
	private readonly ManualResetEvent _terminationGate;

	public CoreApplicationExtension(ManualResetEvent terminationGate)
	{
		_terminationGate = terminationGate;
	}

	public bool CanExit => true;

	public void Exit()
	{
		ExitRequested = true;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application has requested an exit");
		}

		_terminationGate.Set();
	}

	/// <summary>
	/// Determines if <see cref="Application.Exit()"/> has been called.
	/// </summary>
	internal bool ExitRequested { get; private set; }
}
