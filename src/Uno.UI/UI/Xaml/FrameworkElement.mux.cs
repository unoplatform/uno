// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FrameworkElement_partial.cpp

using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		internal virtual string GetPlainText() => "";

		internal protected static string GetStringFromObject(object pObject)
		{
			// First, try IFrameworkElement
			var spFrameworkElement = pObject as FrameworkElement;
			if (spFrameworkElement != null)
			{
				return spFrameworkElement.GetPlainText();
			}

			// Try IPropertyValue
			var type = pObject.GetType();

			if (ValueConversionHelpers.CanConvertValueToString(type))
			{
				return ValueConversionHelpers.ConvertValueToString(pObject, pObject.GetType());
			}

			// Try ICustomPropertyProvider
			var spCustomPropertyProvider = pObject as ICustomPropertyProvider;
			if (spCustomPropertyProvider != null)
			{
				return spCustomPropertyProvider.GetStringRepresentation();
			}

			// Finally, Try IStringable
			var spStringable = pObject as IStringable;
			if (spStringable != null)
			{
				return spStringable.ToString();
			}

			//TODO MZ: Should default to null instead of ToString?
			return pObject.ToString() ?? null;
		}

		// Get property value from style.
		internal bool TryGetValueFromStyle(DependencyProperty dp, out object value)
		{
			Style activeStyle = GetActiveStyle();
			if (activeStyle is not null)
			{
				return activeStyle.TryGetPropertyValue(dp, out value, this);
			}

			value = null;
			return false;
		}

	}
}
