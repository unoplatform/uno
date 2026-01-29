// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ColorSpectrumAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ColorSpectrum types to Microsoft UI Automation.
/// </summary>
public partial class ColorSpectrumAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
	public ColorSpectrumAutomationPeer(ColorSpectrum owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Value)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Slider;

	protected override string GetLocalizedControlTypeCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_LocalizedControlTypeColorSpectrum);

	protected override string GetNameCore()
	{
		var nameString = base.GetNameCore();

		// If a name hasn't been provided by AutomationProperties.Name in markup,
		// then we'll return the default value.
		if (string.IsNullOrEmpty(nameString))
		{
			nameString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameColorSpectrum);
		}

		return nameString;
	}

	protected override string GetClassNameCore()
		=> nameof(ColorSpectrum);

	protected override string GetHelpTextCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_HelpTextColorSpectrum);

	protected override Rect GetBoundingRectangleCore()
	{
		var boundingRectangle = ColorSpectrumOwner.GetBoundingRectangle();
		return boundingRectangle;
	}

	protected override Point GetClickablePointCore()
	{
		var boundingRect = GetBoundingRectangleCore(); // Call potentially overridden method

		return new Point(boundingRect.X + boundingRect.Width / 2, boundingRect.Y + boundingRect.Height / 2);
	}

	// IValueProvider properties and methods
	public bool IsReadOnly => false;

	public string Value
	{
		get
		{
			var colorSpectrumOwner = ColorSpectrumOwner;
			var color = colorSpectrumOwner.Color;
			var hsvColor = colorSpectrumOwner.HsvColor;

			return GetValueString(color, hsvColor);
		}
	}

	public void SetValue(string value)
	{
		var colorSpectrumOwner = ColorSpectrumOwner;
		var color = (Color)XamlBindingHelper.ConvertValue(typeof(Color), value);

		colorSpectrumOwner.Color = color;

		// Since ColorPicker sets ColorSpectrum.Color and ColorPicker also responds to ColorSpectrum.ColorChanged,
		// we could get into an infinite loop if we always raised ColorSpectrum.ColorChanged when ColorSpectrum.Color changed.
		// Because of that, we'll raise the event manually.
		colorSpectrumOwner.RaiseColorChanged();
	}

	internal void RaisePropertyChangedEvent(Color oldColor, Color newColor, Vector4 oldHsvColor, Vector4 newHsvColor)
	{
		var oldValueString = GetValueString(oldColor, oldHsvColor);
		var newValueString = GetValueString(newColor, newHsvColor);

		base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueString, newValueString);
	}

	private static string GetValueString(Color color, Vector4 hsvColor)
	{
		var hue = (uint)Math.Round(Hsv.GetHue(hsvColor));
		var saturation = (uint)Math.Round(Hsv.GetSaturation(hsvColor) * 100);
		var value = (uint)Math.Round(Hsv.GetValue(hsvColor) * 100);

		if (DownlevelHelper.ToDisplayNameExists())
		{
			return StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringColorSpectrumWithColorName),
				ColorHelper.ToDisplayName(color),
				hue, saturation, value);
		}

		return StringUtil.FormatString(
			ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringColorSpectrumWithoutColorName),
			hue, saturation, value);
	}

	private ColorSpectrum ColorSpectrumOwner => (ColorSpectrum)Owner;
}
