// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\core\core\elements\Resources.cpp, tag winui3/release/1.4.2, lines 2288-2374

using Windows.UI;

namespace Microsoft.UI.Xaml;

partial class ColorPaletteResources
{
	/// <summary>
	/// Gets the color value for the specified property from the dictionary.
	/// </summary>
	/// <param name="property">The color palette property to retrieve.</param>
	/// <returns>The color value if found; otherwise, null.</returns>
	private Color? GetColor(ColorPaletteProperty property)
	{
		var key = s_propertyToKeyMap[property];
		if (TryGetValue(key, out var value, shouldCheckSystem: false) && value is Color color)
		{
			return color;
		}

		return null;
	}

	/// <summary>
	/// Sets the color value for the specified property in the dictionary.
	/// </summary>
	/// <param name="property">The color palette property to set.</param>
	/// <param name="value">The color value to set, or null to remove the color.</param>
	private void SetColor(ColorPaletteProperty property, Color? value)
	{
		var key = s_propertyToKeyMap[property];

		// Remove existing entry if present
		Remove(key);

		if (value.HasValue)
		{
			this[key] = value.Value;
			_overridesFromProperties.Add(property);
		}
		else
		{
			_overridesFromProperties.Remove(property);
		}
	}
}
