using Uno.UI.Runtime.Skia;

namespace SamplesApp.Skia.Tizen
{
	class MainClass
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new SamplesApp.App(), args);
			SampleControl.Presentation.SampleChooserViewModel.TakeScreenShot = filePath => host.TakeScreenshot(filePath);
			host.Run();
		}
	}
}
