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

namespace Test01;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}
}

[Microsoft.UI.Xaml.Markup.MarkupExtensionReturnType(ReturnType = typeof(string))]
public class Simple : Microsoft.UI.Xaml.Markup.MarkupExtension
{
	public string TextValue { get; set; }

	protected override object ProvideValue()
	{
		return TextValue + " markup extension";
	}
}
