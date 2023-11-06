using System;
using System.Globalization;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public class ColorPickerSliderAutomationPeer : SliderAutomationPeer, IValueProvider
	{
		public ColorPickerSliderAutomationPeer(ColorPickerSlider owner) : base(owner)
		{
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			// If this slider is handling the alpha channel, then we don't want to do anything special for it -
			// in that case, we'll just return the base SliderAutomationPeer.
			if ((Owner as ColorPickerSlider).ColorChannel != ColorPickerHsvChannel.Alpha && patternInterface == PatternInterface.Value)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		bool IValueProvider.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		string IValueProvider.Value
		{
			get
			{
				ColorPickerSlider owner = Owner as ColorPickerSlider;
				DependencyObject currentObject = owner;
				ColorPicker owningColorPicker = null;

				// Uno Doc: Re-written in C# to avoid assignment during while expression evaluation
				while (currentObject != null)
				{
					owningColorPicker = currentObject as ColorPicker;

					if (owningColorPicker != null) { break; }

					currentObject = VisualTreeHelper.GetParent(currentObject);
				}

				if (owningColorPicker != null)
				{
					Color color = owningColorPicker.Color;
					double sliderValue = owner.Value;

					return GetValueString(color, (int)(Math.Round(sliderValue)));
				}
				else
				{
					return string.Empty;
				}
			}
		}

		public void SetValue(string value)
		{
			// Uno Doc: Not used so commented out
			// MUX_ASSERT(false); // Not implemented.
		}

		public void RaisePropertyChangedEvent(Color oldColor, Color newColor, int oldValue, int newValue)
		{
			// Uno Doc: ValuePatternIdentifiers.ValueProperty is not implemented
			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Automation.ValuePatternIdentifiers", nameof(ValuePatternIdentifiers.ValueProperty)))
			{
				string oldValueString = GetValueString(oldColor, oldValue);
				string newValueString = GetValueString(newColor, newValue);

				base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueString, newValueString);
			}
		}

		private string GetValueString(Color color, int value)
		{
			string resourceStringWithName;
			switch ((Owner as ColorPickerSlider).ColorChannel)
			{
				case ColorPickerHsvChannel.Hue:
					resourceStringWithName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringHueSliderWithColorName);
					break;
				case ColorPickerHsvChannel.Saturation:
					resourceStringWithName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringSaturationSliderWithColorName);
					break;
				case ColorPickerHsvChannel.Value:
					resourceStringWithName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringValueSliderWithColorName);
					break;
				default:
					return string.Empty;
			}

			return StringUtil.FormatString(
				resourceStringWithName,
				value,
				// Uno Doc: ColorHelper.ToDisplayName is not implemented
				"");
				// ColorHelper.ToDisplayName(color));
		}
	}
}
