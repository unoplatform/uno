
namespace Uno.UI.Runtime.Skia;

public abstract class SkiaHost
{
	internal Action? AfterInit { get; set; }

	public void Run()
	{
		Initialize();
		AfterInit?.Invoke();
		RunLoop();
	}

	protected abstract void Initialize();

	protected abstract void RunLoop();

	// public void TakeScreenshot(string filePath)
}
