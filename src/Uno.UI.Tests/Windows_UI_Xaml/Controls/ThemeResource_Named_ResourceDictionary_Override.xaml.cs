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

namespace Uno.UI.Tests.Windows_UI_Xaml.Controls;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ThemeResource_Named_ResourceDictionary_Override : Page
{
	public ThemeResource_Named_ResourceDictionary_Override()
	{
		this.InitializeComponent();
	}
}

public class ThemeResource_Named_ResourceDictionary_Override_Custom : ResourceDictionary
{
	private const string GreenUri = "ms-appx:///Windows_UI_Xaml/Controls/ThemeResource_Named_ResourceDictionary_Override_Green.xaml";
	private const string RedUri = "ms-appx:///Windows_UI_Xaml/Controls/ThemeResource_Named_ResourceDictionary_Override_Red.xaml";
	private const string BrushUri = "ms-appx:///Windows_UI_Xaml/Controls/ThemeResource_Named_ResourceDictionary_Override_MyBrush.xaml";
	private static string ColorUri = string.Empty;

	private string _selectedColorUri = string.Empty;

	public ThemeResource_Named_ResourceDictionary_Override_Custom()
	{
		// Toggle whether we use MyColorGreen.xaml or MyColorRed.xaml as the merged-dict override
		_selectedColorUri = ColorUri = ColorUri == RedUri ? GreenUri : RedUri;

		// Instantiating this dictionary multiple times in different XAML scopes
		// should not force the inner merged dictionaries to the first instance ever
		// created for "BrushUri".
		var myBrush = new ResourceDictionary { Source = new Uri(BrushUri) };

		myBrush.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ColorUri) });

		this.MergedDictionaries.Add(myBrush);
	}
}
