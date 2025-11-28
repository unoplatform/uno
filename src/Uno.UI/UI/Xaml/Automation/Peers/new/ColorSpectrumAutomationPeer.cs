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

public partial class ColorSpectrumAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
	public ColorSpectrumAutomationPeer(ColorSpectrum owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.Value ? this : base.GetPatternCore(patternInterface);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Slider;

	protected override string GetLocalizedControlTypeCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_LocalizedControlTypeColorSpectrum);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		return string.IsNullOrEmpty(name)
			? ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameColorSpectrum)
			: name;
	}

	protected override string GetClassNameCore()
		=> nameof(ColorSpectrum);

	protected override string GetHelpTextCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_HelpTextColorSpectrum);

	protected override Rect GetBoundingRectangleCore()
		=> ColorSpectrumOwner.GetBoundingRectangle();

	protected override Point GetClickablePointCore()
	{
		var bounds = GetBoundingRectangleCore();
		return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
	}

	public bool IsReadOnly => false;

	public string Value
	{
		get
		{
			var owner = ColorSpectrumOwner;
			return GetValueString(owner.Color, owner.HsvColor);
		}
	}

	public void SetValue(string value)
	{
		var owner = ColorSpectrumOwner;
		var color = (Color)XamlBindingHelper.ConvertValue(typeof(Color), value);
		owner.Color = color;
		owner.RaiseColorChanged();
	}

	public void RaisePropertyChangedEvent(Color oldColor, Color newColor, Vector4 oldHsvColor, Vector4 newHsvColor)
	{
		if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Automation.ValuePatternIdentifiers, Uno.UI", nameof(ValuePatternIdentifiers.ValueProperty)))
		{
			var oldValue = GetValueString(oldColor, oldHsvColor);
			var newValue = GetValueString(newColor, newHsvColor);
			base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
		}
	}

	private string GetValueString(Color color, Vector4 hsvColor)
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
