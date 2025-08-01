using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ThemeResource_Theme_Changing_Override : Page
{
	public ThemeResource_Theme_Changing_Override()
	{
		this.InitializeComponent();
	}
}

public class ThemeResource_Theme_Changing_Override_Custom : ResourceDictionary
{
	private const string GreenUri = "ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ThemeResource_TCO_MyColorGreen.xaml";
	private const string RedUri = "ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ThemeResource_TCO_MyColorRed.xaml";
	private const string BrushUri = "ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ThemeResource_TCO_MyBrush.xaml";
	private const string AliasUri = "ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ThemeResource_TCO_MyAlias.xaml";
	private const string ButtonUri = "ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ThemeResource_TCO_MyButton.xaml";

	private string _mode;

	public string Mode
	{
		get => _mode;
		set
		{
			_mode = value;
			var colorUri = _mode == "Green" ? GreenUri : RedUri;

			var myBrush = new ResourceDictionary { Source = new Uri(BrushUri) };
			var myAlias = new ResourceDictionary { Source = new Uri(AliasUri) };
			var myButton = new ResourceDictionary { Source = new Uri(ButtonUri) };

			myBrush.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(colorUri) });
			myAlias.MergedDictionaries.Add(myBrush);
			myButton.MergedDictionaries.Add(myAlias);

			MergedDictionaries.Add(myButton);
		}
	}
}
