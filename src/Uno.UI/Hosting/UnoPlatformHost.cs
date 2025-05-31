#nullable enable

using System;
using System.Threading.Tasks;

namespace Uno.UI.Hosting;

public abstract class UnoPlatformHost
{
	internal Action? AfterInit { get; set; }

	private Task RunCore()
	{
		Initialize();
		AfterInit?.Invoke();
		return RunLoop();
	}

	public void Run()
	{
		var task = RunCore();
		if (task != Task.CompletedTask)
		{
			throw new InvalidOperationException($"Running host {this} requires calling 'await host.RunAsync()' instead of 'host.Run()'.");
		}
	}

	public async Task RunAsync() => await RunCore();

	protected abstract void Initialize();

	protected abstract Task RunLoop();
}
