#nullable enable

using System;
using System.Threading.Tasks;

namespace Uno.UI.Hosting;

public abstract class UnoPlatformHost
{
	internal Action? AfterInitAction { get; set; }

	private async Task RunCore()
	{
		Initialize();
		await InitializeAsync();
		AfterInitAction?.Invoke();
		await RunLoop();
	}

	public void Run()
	{
		var task = RunCore();
		if (task.IsFaulted)
		{
			task.GetAwaiter().GetResult();
		}
		else if (!task.IsCompleted)
		{
			throw new InvalidOperationException($"Running host {this} requires calling 'await host.RunAsync()' instead of 'host.Run()'.");
		}
	}

	public async Task RunAsync() => await RunCore();

	protected abstract void Initialize();

	protected virtual Task InitializeAsync() => Task.CompletedTask;

	protected abstract Task RunLoop();
}
