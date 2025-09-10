using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;
using Windows.UI;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
#endif

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ColorPickerSliderAutomationPeer : SliderAutomationPeer, IValueProvider
	{
		internal ColorPickerSliderAutomationPeer(ColorPickerSlider owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			// If this slider is handling the alpha channel, then we don't want to do anything special for it -
			// in that case, we'll just return the base SliderAutomationPeer.
			if (((ColorPickerSlider)Owner).ColorChannel != ColorPickerHsvChannel.Alpha && patternInterface == PatternInterface.Value)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		// IValueProvider properties and methods
		bool IValueProvider.IsReadOnly => false;

		string IValueProvider.Value
		{
			get
			{
				ColorPickerSlider owner = (ColorPickerSlider)Owner;
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

					return GetValueString(color, (Int32)(Math.Round(sliderValue)));
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
			if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Automation.ValuePatternIdentifiers, Uno.UI", nameof(ValuePatternIdentifiers.ValueProperty)))
			{
				string oldValueString = GetValueString(oldColor, oldValue);
				string newValueString = GetValueString(newColor, newValue);

				base.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValueString, newValueString);
			}
		}

		private string GetValueString(Color color, int value)
		{
			if (DownlevelHelper.ToDisplayNameExists())
			{
				string resourceStringWithName;
				switch (((ColorPickerSlider)Owner).ColorChannel)
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
					ColorHelper.ToDisplayName(color));
			}
			else
			{
				string resourceStringWithoutName;
				switch (((ColorPickerSlider)Owner).ColorChannel)
				{
					case ColorPickerHsvChannel.Hue:
						resourceStringWithoutName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringHueSliderWithoutColorName);
						break;
					case ColorPickerHsvChannel.Saturation:
						resourceStringWithoutName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringSaturationSliderWithoutColorName);
						break;
					case ColorPickerHsvChannel.Value:
						resourceStringWithoutName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ValueStringValueSliderWithoutColorName);
						break;
					default:
						return string.Empty;
				}

				return StringUtil.FormatString(
					resourceStringWithoutName,
					value);
			}
		}
	}
}
