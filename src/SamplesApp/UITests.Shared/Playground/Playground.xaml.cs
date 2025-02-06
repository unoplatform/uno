using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Storage;
#if __WASM__
using Monaco;
#endif

namespace UITests.Playground;

[SampleControlInfo("Playground", "Playground", ignoreInSnapshotTests: true)]
public sealed partial class Playground : UserControl
{
#if __WASM__
	private CodeEditor _codeEditor;
#endif

	public Playground()
	{
		this.InitializeComponent();
#if __WASM__
		this.Loaded += OnLoaded;
#endif
#if !__WASM__
		xamlText.Text = ApplicationData.Current.LocalSettings.Values["PlaygroundXaml"] as string ?? "";
#endif
	}

#if __WASM__
	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(100); // Workaround for https://github.com/unoplatform/uno/issues/15374
		_codeEditor = new CodeEditor()
		{
			Background = new SolidColorBrush(Windows.UI.Colors.Transparent),
			HasGlyphMargin = true,
			Text = ApplicationData.Current.LocalSettings.Values["PlaygroundXaml"] as string ?? "",
			CodeLanguage = "XML"
		};
		_codeEditor.Loaded += OnXamlEditorLoaded;
		MonacoContainer.Children.Add(_codeEditor);
	}

	private async void OnXamlEditorLoaded(object sender, RoutedEventArgs e)
	{
		_codeEditor.CodeLanguage = "xml";
	}
#endif

	private string GetEditorText()
	{
#if __WASM__
		return _codeEditor.Text;
#else
		return xamlText.Text;
#endif
	}

	private string GetXamlInput()
	{
		var ns = new[] {
				("behaviors", "using:Uno.UI.Demo.Behaviors"),
				("utu", "using:Uno.Toolkit.UI"),
				("muxc", "using:Windows.UI.Xaml.Controls"),
				("um", "using:Uno.Material.Extensions"),
				("mtuc", "using:Microsoft.Toolkit.Uwp.UI.Controls"),
				("mtud", "using:Microsoft.Toolkit.Uwp.DeveloperTools"),
				("mtul", "using:Microsoft.Toolkit.Uwp.UI.Lottie"),
				("x", "http://schemas.microsoft.com/winfx/2006/xaml"),
				("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006"),
				("d", "http://schemas.microsoft.com/expression/blend/2008")
			};

		var nsTags = string.Join(" ", ns.Select(v => $"xmlns:{v.Item1}=\"{v.Item2}\""));


		return
			$@"<Grid
				xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
				{nsTags}>
			{GetEditorText()}
			</Grid>";

	}

	private void OnApply()
	{
		try
		{
			ApplicationData.Current.LocalSettings.Values["PlaygroundXaml"] = GetEditorText();
			renderSurface.Content = XamlReader.Load(GetXamlInput());
		}
		catch (Exception e)
		{
			renderSurface.Content = e;
		}
	}
}
