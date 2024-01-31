using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MUXControlsTestApp;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace UITests.Playground;

[SampleControlInfo("Playground", "Playground", ignoreInSnapshotTests: true)]
public sealed partial class Playground : UserControl
{
	public Playground()
	{
		this.InitializeComponent();
		xamlText.Text = ApplicationData.Current.LocalSettings.Values["PlaygroundXaml"] as string ?? "";
	}

#if __WASM__
	private async void OnXamlEditorLoaded(object sender, RoutedEventArgs e)
	{
		xamlText.CodeLanguage = "xml";
	}	
#endif

	private string GetXamlInput()
	{
		var ns = new[] {
				("behaviors", "using:Uno.UI.Demo.Behaviors"),
				("utu", "using:Uno.Toolkit.UI"),
				("muxc", "using:Microsoft.UI.Xaml.Controls"),
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
			{xamlText.Text}
			</Grid>";

	}

	private void OnApply()
	{
		try
		{
			ApplicationData.Current.LocalSettings.Values["PlaygroundXaml"] = xamlText.Text;
			renderSurface.Content = XamlReader.Load(GetXamlInput());
		}
		catch (Exception e)
		{
			renderSurface.Content = e;
		}
	}
}
