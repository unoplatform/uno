using System;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;

// Workaround a net9 nuget bug where :
// <Import Project="$(NuGetPackageRoot)/uno.fonts.fluent/2.6.1/buildTransitive/Uno.Fonts.Fluent.props" Condition="Exists('$(NuGetPackageRoot)/uno.fonts.fluent/2.6.1/buildTransitive/Uno.Fonts.Fluent.props')" />
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


var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();