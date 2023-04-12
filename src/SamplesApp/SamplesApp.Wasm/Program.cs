using System;
using Windows.UI.Xaml;

namespace SamplesApp.Wasm
{
	public class Program
	{
		private static App _app;

		public static void Main(string[] args)
		{
			// Required to allow for Puppeteer to select XAML elements in the HTML DOM.
			Uno.UI.FeatureConfiguration.UIElement.AssignDOMXamlName = true;
#if !DEBUG
			Uno.UI.FeatureConfiguration.UIElement.RenderToStringWithId = false;
#endif
#if DEBUG
			Uno.UI.FeatureConfiguration.UIElement.AssignDOMXamlProperties = true;
#endif

			Windows.UI.Xaml.Application.Start(_ => _app = new App());
		}
	}
}
