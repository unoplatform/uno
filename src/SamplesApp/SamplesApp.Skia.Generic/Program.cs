#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia;

namespace SkiaSharpExample
{
	class MainClass
	{
		static SamplesApp.App? _app;

		[STAThread]
		public static void Main(string[] args)
		{
			SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

			var host = SkiaHostBuilder.Create()
				.App(() => _app = new SamplesApp.App())
				.AfterInit(() =>
				{
					if (_app is not null && OperatingSystem.IsWindows())
					{
						_app.MainWindowActivated += delegate
						{
							var windowContent = System.Windows.Application.Current.Windows[0].Content;
							Assert.IsInstanceOfType(windowContent, typeof(System.Windows.UIElement));
							var windowContentAsUIElement = (System.Windows.UIElement)windowContent;
							Assert.IsTrue(windowContentAsUIElement.IsFocused);
						};
					}
				})
				.UseX11()
				.UseLinuxFrameBuffer()
				.UseWindows(b => b
					.WpfApplication(() =>
					{
						// optional app creation
						return new System.Windows.Application();
					}))
				.UseMacOS()
				.Build();

			host.Run();
		}
	}
}
