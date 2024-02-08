namespace Uno.Runtime.Skia;

public abstract class SkiaHost
{
	public void Run()
	{
		Initialize();
		RunLoop();
	}

	protected abstract void Initialize();

	protected abstract void RunLoop();

	// public void TakeScreenshot(string filePath)
}
