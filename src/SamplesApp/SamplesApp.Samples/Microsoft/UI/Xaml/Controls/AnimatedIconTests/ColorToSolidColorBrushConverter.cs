// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/dev/AnimatedIcon/TestUI/ColorToSolidColorBrushConverter.cs

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace UITests.Microsoft_UI_Xaml_Controls.AnimatedIconTests
{
	partial class ColorToSolidColorBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return new SolidColorBrush((Color)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return ((SolidColorBrush)value).Color;
		}
	}
}
