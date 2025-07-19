using System;
using System.Numerics;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public class ColorSpectrumAutomationPeer : AutomationPeer, IValueProvider
	{
		// Uno Doc: Added for the Uno Platform
		private readonly ColorSpectrum _owner;

		internal ColorSpectrumAutomationPeer(ColorSpectrum owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides 
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Value)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Slider;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_LocalizedControlTypeColorSpectrum);
		}

		protected override string GetNameCore()
		{
			string nameString = base.GetNameCore();

			// If a name hasn't been provided by AutomationProperties.Name in markup,
			// then we'll return the default value.
			if (string.IsNullOrEmpty(nameString))
			{
				nameString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameColorSpectrum);
			}

			return nameString;
		}

		protected override string GetClassNameCore()
		{
			return nameof(ColorSpectrum);
		}

		protected override string GetHelpTextCore()
		{
			return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_HelpTextColorSpectrum);
		}

		protected override Rect GetBoundingRectangleCore()
		{
			Rect boundingRectangle = _owner.GetBoundingRectangle();
			return boundingRectangle;
		}

		protected override Point GetClickablePointCore()
		{
			var boundingRect = GetBoundingRectangleCore(); // Call potentially overridden method

			return new Point(boundingRect.X + boundingRect.Width / 2, boundingRect.Y + boundingRect.Height / 2);
		}

		// IValueProvider properties and methods
		public bool IsReadOnly
		{
			get => false;
		}

		public string Value
		{
			get
			{
				ColorSpectrum colorSpectrumOwner = _owner;
				Color color = colorSpectrumOwner.Color;
				Vector4 hsvColor = colorSpectrumOwner.HsvColor;

				return GetValueString(color, hsvColor);
			}
		}

		public void SetValue(string value)
		{
			ColorSpectrum colorSpectrumOwner = _owner;
			Color color = (Color)(XamlBindingHelper.ConvertValue(typeof(Color), value));

			colorSpectrumOwner.Color = color;

			// Since ColorPicker sets ColorSpectrum.Color and ColorPicker also responds to ColorSpectrum.ColorChanged,
			// we could get into an infinite loop if we always raised ColorSpectrum.ColorChanged when ColorSpectrum.Color changed.
			// Because of that, we'll raise the event manually.
			colorSpectrumOwner.RaiseColorChanged();
		}

		public void RaisePropertyChangedEvent(Color oldColor, Color newColor, Vector4 oldHsvColor, Vector4 newHsvColor)
		{
			if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Automation.ValuePatternIdentifiers", nameof(ValuePatternIdentifiers.ValueProperty)))
			{
				string oldValueString = GetValueString(oldColor, oldHsvColor);
				string newValueString = GetValueString(newColor, newHsvColor);

				base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueString, newValueString);
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
			else
			{
				return StringUtil.FormatString(
					ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringColorSpectrumWithoutColorName),
					hue, saturation, value);
			}
		}
	}
}
