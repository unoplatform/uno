using System;
using System.Globalization;
using Uno.UI.Core;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class ColorPickerSlider : Slider
	{
		private ToolTip m_toolTip;

		public ColorPickerSlider()
		{
			// We want the ColorPickerSlider to pick up everything for its default style from the Slider's default style,
			// since its purpose is just to turn off keyboarding.  So we'll give it Slider's control name as its default style key
			// instead of ColorPickerSlider.
			DefaultStyleKey = typeof(Microsoft.UI.Xaml.Controls.Slider);

			ValueChanged += OnValueChangedEvent;
		}

		// IUIElementOverridesHelper overrides
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ColorPickerSliderAutomationPeer(this);
		}

		// IFrameworkElementOverrides overrides
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			m_toolTip = GetTemplateChild<ToolTip>("ToolTip");

			if (m_toolTip is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();
			}
		}

		// IControlOverrides overrides
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			if (args.Key != VirtualKey.Left &&
				args.Key != VirtualKey.Right &&
				args.Key != VirtualKey.Up &&
				args.Key != VirtualKey.Down)
			{
				base.OnKeyDown(args);
				return;
			}

			ColorPicker parentColorPicker = GetParentColorPicker();

			if (parentColorPicker == null)
			{
				return;
			}

			bool isControlDown = (KeyboardStateTracker.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			double minBound = 0;
			double maxBound = 0;

			Hsv currentHsv = parentColorPicker.GetCurrentHsv();
			double currentAlpha = 0;

			switch (this.ColorChannel)
			{
				case ColorPickerHsvChannel.Hue:
					minBound = parentColorPicker.MinHue;
					maxBound = parentColorPicker.MaxHue;
					currentHsv.H = this.Value;
					break;

				case ColorPickerHsvChannel.Saturation:
					minBound = parentColorPicker.MinSaturation;
					maxBound = parentColorPicker.MaxSaturation;
					currentHsv.S = this.Value / 100;
					break;

				case ColorPickerHsvChannel.Value:
					minBound = parentColorPicker.MinValue;
					maxBound = parentColorPicker.MaxValue;
					currentHsv.V = this.Value / 100;
					break;

				case ColorPickerHsvChannel.Alpha:
					minBound = 0;
					maxBound = 100;
					currentAlpha = this.Value / 100;
					break;

				default:
					throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'throw winrt::hresult_error(E_FAIL);'
			}

			bool shouldInvertHorizontalDirection = this.FlowDirection == FlowDirection.RightToLeft && !this.IsDirectionReversed;

			ColorHelpers.IncrementDirection direction =
				((args.Key == VirtualKey.Left && !shouldInvertHorizontalDirection) ||
				 (args.Key == VirtualKey.Right && shouldInvertHorizontalDirection) ||
				  args.Key == VirtualKey.Down) ?
				ColorHelpers.IncrementDirection.Lower :
				ColorHelpers.IncrementDirection.Higher;

			ColorHelpers.IncrementAmount amount = isControlDown ? ColorHelpers.IncrementAmount.Large : ColorHelpers.IncrementAmount.Small;

			if (this.ColorChannel != ColorPickerHsvChannel.Alpha)
			{
				currentHsv = ColorHelpers.IncrementColorChannel(currentHsv, this.ColorChannel, direction, amount, false /* shouldWrap */, minBound, maxBound);
			}
			else
			{
				currentAlpha = ColorHelpers.IncrementAlphaChannel(currentAlpha, direction, amount, false /* shouldWrap */, minBound, maxBound);
			}

			switch (this.ColorChannel)
			{
				case ColorPickerHsvChannel.Hue:
					this.Value = currentHsv.H;
					break;

				case ColorPickerHsvChannel.Saturation:
					this.Value = currentHsv.S * 100;
					break;

				case ColorPickerHsvChannel.Value:
					this.Value = currentHsv.V * 100;
					break;

				case ColorPickerHsvChannel.Alpha:
					this.Value = currentAlpha * 100;
					break;

				default:
					throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'MUX_ASSERT(false);'
			}

			args.Handled = true;
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			if (m_toolTip is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();
				toolTip.IsEnabled = true;
				toolTip.IsOpen = true;
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (m_toolTip is ToolTip toolTip)
			{
				toolTip.IsOpen = false;
			}
		}

		private void OnValueChangedEvent(object sender, RangeBaseValueChangedEventArgs args)
		{
			if (m_toolTip is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();

				// ToolTip doesn't currently provide any way to re-run its placement logic if its placement target moves,
				// so toggling IsEnabled induces it to do that without incurring any visual glitches.
				toolTip.IsEnabled = false;
				toolTip.IsEnabled = true;
			}

			DependencyObject currentObject = this;
			ColorPicker owningColorPicker = null;

			while (currentObject != null && (owningColorPicker = currentObject as ColorPicker) == null)
			{
				currentObject = VisualTreeHelper.GetParent(currentObject);
			}

			if (owningColorPicker != null)
			{
				Color oldColor = owningColorPicker.Color;
				Hsv hsv = ColorConversion.RgbToHsv(ColorConversion.RgbFromColor(oldColor));
				hsv.V = args.NewValue / 100.0;
				Color newColor = ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(hsv));

				ColorPickerSliderAutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this) as ColorPickerSliderAutomationPeer;
				peer.RaisePropertyChangedEvent(oldColor, newColor, (int)(Math.Round(args.OldValue)), (int)(Math.Round(args.NewValue)));
			}
		}

		private ColorPicker GetParentColorPicker()
		{
			ColorPicker parentColorPicker = null;
			DependencyObject currentObject = this;

			while (currentObject != null && !(parentColorPicker is ColorPicker))
			{
				currentObject = VisualTreeHelper.GetParent(currentObject);
			}

			return parentColorPicker;
		}

		private string GetToolTipString()
		{
			uint sliderValue = (uint)(Math.Round(this.Value));

			if (this.ColorChannel == ColorPickerHsvChannel.Alpha)
			{
				return StringUtil.FormatString(
					ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringAlphaSlider),
					sliderValue);
			}
			else
			{
				ColorPicker parentColorPicker = GetParentColorPicker();
				if (parentColorPicker != null && DownlevelHelper.ToDisplayNameExists())
				{
					Hsv currentHsv = parentColorPicker.GetCurrentHsv();
					string localizedString;

					switch (this.ColorChannel)
					{
						case ColorPickerHsvChannel.Hue:
							currentHsv.H = this.Value;
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringHueSliderWithColorName);
							break;

						case ColorPickerHsvChannel.Saturation:
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringSaturationSliderWithColorName);
							currentHsv.S = this.Value / 100;
							break;

						case ColorPickerHsvChannel.Value:
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringValueSliderWithColorName);
							currentHsv.V = this.Value / 100;
							break;
						default:
							throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'throw winrt::hresult_error(E_FAIL);'
					}

					return StringUtil.FormatString(
						localizedString,
						sliderValue,
						ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(currentHsv))));
				}
				else
				{
					string localizedString;
					switch (this.ColorChannel)
					{
						case ColorPickerHsvChannel.Hue:
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringHueSliderWithoutColorName);
							break;
						case ColorPickerHsvChannel.Saturation:
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringSaturationSliderWithoutColorName);
							break;
						case ColorPickerHsvChannel.Value:
							localizedString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ToolTipStringValueSliderWithoutColorName);
							break;
						default:
							throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'throw winrt::hresult_error(E_FAIL);'
					}

					return StringUtil.FormatString(
						localizedString,
						sliderValue);
				}
			}
		}
	}
}
