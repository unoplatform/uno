using System;
using Windows.UI.Xaml;

namespace SamplesApp.Wasm
{
	public class Program
	{
		private static App _app;

		public static void Main(string[] args)
		{
			// Workaround a net9 nuget bug where :
			// <Import Project="$(NuGetPackageRoot)/uno.fonts.fluent/2.4.5/buildTransitive/Uno.Fonts.Fluent.props" Condition="Exists('$(NuGetPackageRoot)/uno.fonts.fluent/2.4.5/buildTransitive/Uno.Fonts.Fluent.props')" />
			// is not imported properly. Linux only?
			Uno.UI.FeatureConfiguration.Font.SymbolsFont = "ms-appx:///Uno.Fonts.Fluent/Fonts/uno-fluentui-assets.ttf";

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
