using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NativeMethods = __Windows.UI.ViewManagement.ApplicationViewTitleBar.NativeMethods;

namespace Uno.UI.ViewManagement.Helpers;

internal static class TitleBarHelper
{
	private static Color? _backgroundColor;

	public static Color? BackgroundColor
	{
		get => _backgroundColor;
		set
		{
			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				UpdateBackgroundColor();
			}
		}
	}

	private static void UpdateBackgroundColor()
	{
		NativeMethods.SetBackgroundColor(_backgroundColor?.ToHexString());
	}
}
