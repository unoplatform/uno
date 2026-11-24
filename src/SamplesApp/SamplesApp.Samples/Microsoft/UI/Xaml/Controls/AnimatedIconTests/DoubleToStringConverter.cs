// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/dev/AnimatedIcon/TestUI/DoubleToStringConverter.cs

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace UITests.Microsoft_UI_Xaml_Controls.AnimatedIconTests
{
	partial class DoubleToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			double parsedValue = 0.0;
			return Double.TryParse((string)value, out parsedValue) ? parsedValue * 100 : 0.0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return (((double)value) / 100).ToString();
		}
	}
}
