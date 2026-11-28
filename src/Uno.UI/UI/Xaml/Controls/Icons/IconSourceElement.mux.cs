// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.cpp

#nullable enable

using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

partial class IconSourceElement
{
	private protected override void ApplyTemplate(out bool fAddedVisuals)
	{
		fAddedVisuals = false;

		if (GetChildElementNoRef() is null && IconSource is not null)
		{
			SwapIconElements();
			fAddedVisuals = true;
		}
	}

	private void SwapIconElements()
	{
		m_iconElement = null;

		RemoveChildElement();

		var iconSource = IconSource;
		if (iconSource is not null)
		{
			if (iconSource is FontIconSource)
			{
				m_iconElement = new FontIcon();

				SetBinding(iconSource, "Glyph", m_iconElement, FontIcon.GlyphProperty);
				SetBinding(iconSource, "FontFamily", m_iconElement, FontIcon.FontFamilyProperty);
				SetBinding(iconSource, "FontSize", m_iconElement, FontIcon.FontSizeProperty);
				SetBinding(iconSource, "FontStyle", m_iconElement, FontIcon.FontStyleProperty);
				SetBinding(iconSource, "FontWeight", m_iconElement, FontIcon.FontWeightProperty);
				SetBinding(iconSource, "IsTextScaleFactorEnabled", m_iconElement, FontIcon.IsTextScaleFactorEnabledProperty);
				SetBinding(iconSource, "MirroredWhenRightToLeft", m_iconElement, FontIcon.MirroredWhenRightToLeftProperty);
			}
			else if (iconSource is SymbolIconSource)
			{
				m_iconElement = new SymbolIcon();

				SetBinding(iconSource, "Symbol", m_iconElement, SymbolIcon.SymbolProperty);
			}
			else if (iconSource is BitmapIconSource)
			{
				m_iconElement = new BitmapIcon();

				SetBinding(iconSource, "UriSource", m_iconElement, BitmapIcon.UriSourceProperty);
				SetBinding(iconSource, "ShowAsMonochrome", m_iconElement, BitmapIcon.ShowAsMonochromeProperty);
			}
			else if (iconSource is PathIconSource)
			{
				m_iconElement = new PathIcon();

				SetBinding(iconSource, "Data", m_iconElement, PathIcon.DataProperty);
			}

			// Here is a typical visualtree structure when we are using XamlUICommand in AppBarButton
			//  AppBarButton
			//      - IconElement
			//          - IconSource
			// If IconSource is default Foreground, we assume the foreground should be inherited from it's parent AppBarButton, the disabled foreground should apply to icon too.
			// But if we bind iconSource.Foreground with iconElement.Foreground in this situation, any visualstate change to the icon would be override by iconSource.Foreground
			// So we only SetBinding when IconSource.Foreground is not the default one.
			if (m_iconElement is not null && iconSource.GetCurrentHighestValuePrecedence(IconSource.ForegroundProperty) < DependencyPropertyValuePrecedences.DefaultValue)
			{
				SetBinding(iconSource, "Foreground", m_iconElement, IconElement.ForegroundProperty);
			}

			if (m_iconElement is not null)
			{
				AddChildElement(m_iconElement);
			}
		}
	}

	// Equivalent of FxCallbacks::DXamlCore_SetBinding.
	private static void SetBinding(IconSource source, string path, IconElement target, DependencyProperty targetProperty) =>
		BindingOperations.SetBinding(
			target,
			targetProperty,
			new Binding
			{
				Source = source,
				Path = new(path),
				Mode = BindingMode.OneWay,
			});
}
