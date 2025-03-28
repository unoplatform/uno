using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Extensions
{
	internal static class ElementThemeExtensions
	{
		public static ApplicationTheme? ToApplicationThemeOrDefault(this ElementTheme elementTheme)
			=> elementTheme switch
			{
				ElementTheme.Default => null,
				ElementTheme.Light => ApplicationTheme.Light,
				ElementTheme.Dark => ApplicationTheme.Dark,
				_ => throw new ArgumentException()
			};
	}
}
