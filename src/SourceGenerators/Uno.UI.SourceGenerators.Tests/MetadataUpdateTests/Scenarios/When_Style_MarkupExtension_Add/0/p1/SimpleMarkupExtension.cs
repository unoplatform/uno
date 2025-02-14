using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Test01;

[Windows.UI.Xaml.Markup.MarkupExtensionReturnType(ReturnType = typeof(string))]
public class Simple : Windows.UI.Xaml.Markup.MarkupExtension
{
	public string TextValue { get; set; }

	protected override object ProvideValue()
	{
		return TextValue + " markup extension";
	}
}
