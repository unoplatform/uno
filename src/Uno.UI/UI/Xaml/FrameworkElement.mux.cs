// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FrameworkElement_partial.cpp

using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml
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
				return activeStyle.TryGetPropertyValue(dp, out value);
			}

			value = null;
			return false;
		}

		/// <summary>
		/// Returns the <see cref="BindingExpression"/> that represents the binding on the specified property.
		/// </summary>
		/// <param name="dp">The binding target property from which to retrieve the binding expression.</param>
		/// <returns>The binding expression, or null if the property is not bound.</returns>
		// `new` hides the wider Uno-only DependencyObject member; WinUI declares this on FrameworkElement only.
		public new BindingExpression GetBindingExpression(DependencyProperty dp)
			=> base.GetBindingExpression(dp);

	}
}
