using System;
using System.Globalization;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public class ColorPickerSliderAutomationPeer : AutomationPeer, IValueProvider
	{
		// Uno Doc: Added for the Uno Platform
		private readonly ColorPickerSlider _owner;

		internal ColorPickerSliderAutomationPeer(ColorPickerSlider owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			// If this slider is handling the alpha channel, then we don't want to do anything special for it -
			// in that case, we'll just return the base SliderAutomationPeer.
			if (_owner.ColorChannel != ColorPickerHsvChannel.Alpha && patternInterface == PatternInterface.Value)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
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
				ColorPickerSlider owner = _owner;
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
			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Automation.ValuePatternIdentifiers", nameof(ValuePatternIdentifiers.ValueProperty)))
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
				switch (_owner.ColorChannel)
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
				switch (_owner.ColorChannel)
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
