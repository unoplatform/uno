// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHostWrapperConverter.cs

using System;
using System.Globalization;
using System.Windows.Data;
using Windows.UI.Xaml;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// Dual interface (<see cref="IValueConverter"/> and <see cref="windows.UI.Xaml.Data.IValueConverter"/>),
	/// assumes that the conversion is between a <see cref="UnoXamlHostBase"/> and its wrapped <see cref="UIElement"/>
	/// and attempts to return the correct instance of each.
	/// </summary>
	public class UnoXamlHostWrapperConverter : IValueConverter, Windows.UI.Xaml.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as Windows.UI.Xaml.UIElement)?.GetWrapper();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value as UnoXamlHostBase)?.GetUwpInternalObject();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
