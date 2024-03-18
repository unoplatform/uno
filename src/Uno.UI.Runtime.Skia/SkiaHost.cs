
namespace Uno.UI.Runtime.Skia;

public abstract class SkiaHost
{
	internal Action? AfterInit { get; set; }

	public async Task Run()
	{
		Initialize();
		AfterInit?.Invoke();
		await RunLoop();
	}

	protected abstract void Initialize();

	protected abstract Task RunLoop();
}
