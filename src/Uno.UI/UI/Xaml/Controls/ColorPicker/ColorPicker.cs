using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ColorPicker : Control
	{
		private enum ColorUpdateReason
		{
			InitializingColor,
			ColorPropertyChanged,
			ColorSpectrumColorChanged,
			ThirdDimensionSliderChanged,
			AlphaSliderChanged,
			RgbTextBoxChanged,
			HsvTextBoxChanged,
			AlphaTextBoxChanged,
			HexTextBoxChanged,
		};

		private bool m_updatingColor = false;
		private bool m_updatingControls = false;
		private Rgb m_currentRgb = new Rgb(1.0, 1.0, 1.0);
		private Hsv m_currentHsv = new Hsv(0.0, 1.0, 1.0);
		private string m_currentHex = "#FFFFFFFF";
		private double m_currentAlpha = 1.0;

		private string m_previousString = string.Empty;
		private bool m_isFocusedTextBoxValid = false;

		private bool m_textEntryGridOpened = false;

		// Template parts
		private Primitives.ColorSpectrum m_colorSpectrum;

		private Grid m_colorPreviewRectangleGrid;
		private Rectangle m_colorPreviewRectangle;
		private Rectangle m_previousColorRectangle;
		private ImageBrush m_colorPreviewRectangleCheckeredBackgroundImageBrush;

		private IAsyncAction m_createColorPreviewRectangleCheckeredBackgroundBitmapAction = null;

		private Primitives.ColorPickerSlider m_thirdDimensionSlider;
		private LinearGradientBrush m_thirdDimensionSliderGradientBrush;

		private Primitives.ColorPickerSlider m_alphaSlider;
		private LinearGradientBrush m_alphaSliderGradientBrush;
		private Rectangle m_alphaSliderBackgroundRectangle;
		private ImageBrush m_alphaSliderCheckeredBackgroundImageBrush;

		private IAsyncAction m_alphaSliderCheckeredBackgroundBitmapAction = null;

		private ButtonBase m_moreButton;
		private TextBlock m_moreButtonLabel;

		private ComboBox m_colorRepresentationComboBox;
		private TextBox m_redTextBox;
		private TextBox m_greenTextBox;
		private TextBox m_blueTextBox;
		private TextBox m_hueTextBox;
		private TextBox m_saturationTextBox;
		private TextBox m_valueTextBox;
		private TextBox m_alphaTextBox;
		private TextBox m_hexTextBox;

		private ComboBoxItem m_RgbComboBoxItem;
		private ComboBoxItem m_HsvComboBoxItem;
		private TextBlock m_redLabel;
		private TextBlock m_greenLabel;
		private TextBlock m_blueLabel;
		private TextBlock m_hueLabel;
		private TextBlock m_saturationLabel;
		private TextBlock m_valueLabel;
		private TextBlock m_alphaLabel;

		private SolidColorBrush m_checkerColorBrush;

		// Uno Doc: Added to dispose event handlers
		private bool _isTemplateApplied = false;
		private SerialDisposable _eventSubscriptions = new SerialDisposable();

		public ColorPicker()
		{
			// Uno Doc: Not supported
			//__RP_Marker_ClassById(RuntimeProfiler::ProfId_ColorPicker);

			SetDefaultStyleKey(this);

			Loaded += OnLoaded; // Uno Doc: Added to re-registered disposed event handlers
			Unloaded += OnUnloaded;
		}

		// IFrameworkElementOverrides overrides
		protected override void OnApplyTemplate()
		{
			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = null;

			m_colorSpectrum = GetTemplateChild<Primitives.ColorSpectrum>("ColorSpectrum");

			m_colorPreviewRectangleGrid = GetTemplateChild<Grid>("ColorPreviewRectangleGrid");
			m_colorPreviewRectangle = GetTemplateChild<Rectangle>("ColorPreviewRectangle");
			m_previousColorRectangle = GetTemplateChild<Rectangle>("PreviousColorRectangle");
			m_colorPreviewRectangleCheckeredBackgroundImageBrush = GetTemplateChild<ImageBrush>("ColorPreviewRectangleCheckeredBackgroundImageBrush");

			m_thirdDimensionSlider = GetTemplateChild<Primitives.ColorPickerSlider>("ThirdDimensionSlider");
			m_thirdDimensionSliderGradientBrush = GetTemplateChild<LinearGradientBrush>("ThirdDimensionSliderGradientBrush");

			m_alphaSlider = GetTemplateChild<Primitives.ColorPickerSlider>("AlphaSlider");
			m_alphaSliderGradientBrush = GetTemplateChild<LinearGradientBrush>("AlphaSliderGradientBrush");
			m_alphaSliderBackgroundRectangle = GetTemplateChild<Rectangle>("AlphaSliderBackgroundRectangle");
			m_alphaSliderCheckeredBackgroundImageBrush = GetTemplateChild<ImageBrush>("AlphaSliderCheckeredBackgroundImageBrush");

			m_moreButton = GetTemplateChild<ButtonBase>("MoreButton");

			m_colorRepresentationComboBox = GetTemplateChild<ComboBox>("ColorRepresentationComboBox");

			m_redTextBox = GetTemplateChild<TextBox>("RedTextBox");
			m_greenTextBox = GetTemplateChild<TextBox>("GreenTextBox");
			m_blueTextBox = GetTemplateChild<TextBox>("BlueTextBox");
			m_hueTextBox = GetTemplateChild<TextBox>("HueTextBox");
			m_saturationTextBox = GetTemplateChild<TextBox>("SaturationTextBox");
			m_valueTextBox = GetTemplateChild<TextBox>("ValueTextBox");
			m_alphaTextBox = GetTemplateChild<TextBox>("AlphaTextBox");
			m_hexTextBox = GetTemplateChild<TextBox>("HexTextBox");

			m_RgbComboBoxItem = GetTemplateChild<ComboBoxItem>("RGBComboBoxItem");
			m_HsvComboBoxItem = GetTemplateChild<ComboBoxItem>("HSVComboBoxItem");
			m_redLabel = GetTemplateChild<TextBlock>("RedLabel");
			m_greenLabel = GetTemplateChild<TextBlock>("GreenLabel");
			m_blueLabel = GetTemplateChild<TextBlock>("BlueLabel");
			m_hueLabel = GetTemplateChild<TextBlock>("HueLabel");
			m_saturationLabel = GetTemplateChild<TextBlock>("SaturationLabel");
			m_valueLabel = GetTemplateChild<TextBlock>("ValueLabel");
			m_alphaLabel = GetTemplateChild<TextBlock>("AlphaLabel");

			m_checkerColorBrush = GetTemplateChild<SolidColorBrush>("CheckerColorBrush");

			// Uno Doc: Extracted event registrations into a separate method, so they can be re-registered on reloading.
			var registrations = SubscribeToEvents();

			if (m_colorSpectrum is Primitives.ColorSpectrum colorSpectrum)
			{
				AutomationProperties.SetName(colorSpectrum, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameColorSpectrum));
			}

			if (m_thirdDimensionSlider is Primitives.ColorPickerSlider thirdDimensionSlider)
			{
				SetThirdDimensionSliderChannel();
			}

			if (m_alphaSlider is Primitives.ColorPickerSlider alphaSlider)
			{
				alphaSlider.ColorChannel = ColorPickerHsvChannel.Alpha;

				AutomationProperties.SetName(alphaSlider, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameAlphaSlider));
			}

			if (m_moreButton is ButtonBase moreButton)
			{
				AutomationProperties.SetName(moreButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameMoreButtonCollapsed));
				AutomationProperties.SetHelpText(moreButton, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_HelpTextMoreButton));

				// Uno Doc: Re-written in C# to avoid assignment in the if condition evaluation
				TextBlock moreButtonLabel = GetTemplateChild<TextBlock>("MoreButtonLabel");
				if (moreButtonLabel != null)
				{
					m_moreButtonLabel = moreButtonLabel;
					moreButtonLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextMoreButtonLabelCollapsed);
				}
			}

			if (m_colorRepresentationComboBox is ComboBox colorRepresentationComboBox)
			{
				AutomationProperties.SetName(colorRepresentationComboBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameColorModelComboBox));
			}

			if (m_redTextBox is TextBox redTextBox)
			{
				AutomationProperties.SetName(redTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameRedTextBox));
			}

			if (m_greenTextBox is TextBox greenTextBox)
			{
				AutomationProperties.SetName(greenTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameGreenTextBox));
			}

			if (m_blueTextBox is TextBox blueTextBox)
			{
				AutomationProperties.SetName(blueTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameBlueTextBox));
			}

			if (m_hueTextBox is TextBox hueTextBox)
			{
				AutomationProperties.SetName(hueTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameHueTextBox));
			}

			if (m_saturationTextBox is TextBox saturationTextBox)
			{
				AutomationProperties.SetName(saturationTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameSaturationTextBox));
			}

			if (m_valueTextBox is TextBox valueTextBox)
			{
				AutomationProperties.SetName(valueTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameValueTextBox));
			}

			if (m_alphaTextBox is TextBox alphaTextBox)
			{
				AutomationProperties.SetName(alphaTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameAlphaTextBox));
			}

			if (m_hexTextBox is TextBox hexTextBox)
			{
				AutomationProperties.SetName(hexTextBox, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameHexTextBox));
			}

			if (m_RgbComboBoxItem is ComboBoxItem rgbComboBoxItem)
			{
				rgbComboBoxItem.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ContentRGBComboBoxItem);
			}

			if (m_HsvComboBoxItem is ComboBoxItem hsvComboBoxItem)
			{
				hsvComboBoxItem.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ContentHSVComboBoxItem);
			}

			if (m_redLabel is TextBlock redLabel)
			{
				redLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextRedLabel);
			}

			if (m_greenLabel is TextBlock greenLabel)
			{
				greenLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextGreenLabel);
			}

			if (m_blueLabel is TextBlock blueLabel)
			{
				blueLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextBlueLabel);
			}

			if (m_hueLabel is TextBlock hueLabel)
			{
				hueLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextHueLabel);
			}

			if (m_saturationLabel is TextBlock saturationLabel)
			{
				saturationLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextSaturationLabel);
			}

			if (m_valueLabel is TextBlock valueLabel)
			{
				valueLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextValueLabel);
			}

			if (m_alphaLabel is TextBlock alphaLabel)
			{
				alphaLabel.Text = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TextAlphaLabel);
			}

			if (m_checkerColorBrush is SolidColorBrush checkerColorBrush)
			{
				checkerColorBrush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, OnCheckerColorChanged);
			}

			CreateColorPreviewCheckeredBackground();
			CreateAlphaSliderCheckeredBackground();
			UpdateVisualState(useTransitions: false);
			InitializeColor();
			UpdatePreviousColorRectangle();

			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = registrations;
			_isTemplateApplied = true;
		}

		// Property changed handler.
		protected void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == ColorProperty)
			{
				OnColorChanged(args);
			}
			else if (property == PreviousColorProperty)
			{
				OnPreviousColorChanged(args);
			}
			else if (property == IsAlphaEnabledProperty)
			{
				OnIsAlphaEnabledChanged(args);
			}
			else if (
				property == IsColorSpectrumVisibleProperty ||
				property == IsColorPreviewVisibleProperty ||
				property == IsColorSliderVisibleProperty ||
				property == IsAlphaSliderVisibleProperty ||
				property == IsMoreButtonVisibleProperty ||
				property == IsColorChannelTextInputVisibleProperty ||
				property == IsAlphaTextInputVisibleProperty ||
				property == IsHexInputVisibleProperty)
			{
				OnPartVisibilityChanged(args);
			}
			else if (property == MinHueProperty ||
				property == MaxHueProperty)
			{
				OnMinMaxHueChanged(args);
			}
			else if (property == MinSaturationProperty ||
				property == MaxSaturationProperty)
			{
				OnMinMaxSaturationChanged(args);
			}
			else if (property == MinValueProperty ||
				property == MaxValueProperty)
			{
				OnMinMaxValueChanged(args);
			}
			else if (property == ColorSpectrumComponentsProperty)
			{
				OnColorSpectrumComponentsChanged(args);
			}
			else if (property == OrientationProperty)
			{
				OnOrientationChanged(args);
			}
		}

		internal Hsv GetCurrentHsv()
		{
			return m_currentHsv;
		}

		private void OnColorChanged(DependencyPropertyChangedEventArgs args)
		{
			// If we're in the process of internally updating the color, then we don't want to respond to the Color property changing,
			// aside from raising the ColorChanged event.
			if (!m_updatingColor)
			{
				Color color = this.Color;

				m_currentRgb = new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0);
				m_currentAlpha = color.A / 255.0;
				m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
				m_currentHex = GetCurrentHexValue();

				UpdateColorControls(ColorUpdateReason.ColorPropertyChanged);
			}

			Color oldColor = (Color)args.OldValue;
			Color newColor = (Color)args.NewValue;

			if (oldColor.A != newColor.A ||
				oldColor.R != newColor.R ||
				oldColor.G != newColor.G ||
				oldColor.B != newColor.B)
			{
				var colorChangedEventArgs = new ColorChangedEventArgs();

				colorChangedEventArgs.OldColor = oldColor;
				colorChangedEventArgs.NewColor = newColor;

				this.ColorChanged?.Invoke(this, colorChangedEventArgs);
			}
		}

		private void OnPreviousColorChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdatePreviousColorRectangle();
			UpdateVisualState(useTransitions: true);
		}

		private void OnIsAlphaEnabledChanged(DependencyPropertyChangedEventArgs args)
		{
			m_currentHex = GetCurrentHexValue();

			if (m_hexTextBox is TextBox hexTextBox)
			{
				m_updatingControls = true;
				hexTextBox.Text = m_currentHex;
				m_updatingControls = false;
			}

			OnPartVisibilityChanged(args);
		}

		private void OnPartVisibilityChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualState(useTransitions: true);
		}

		private void OnMinMaxHueChanged(DependencyPropertyChangedEventArgs args)
		{
			int minHue = this.MinHue;
			int maxHue = this.MaxHue;

			if (minHue < 0 || minHue > 359)
			{
				throw new ArgumentException("MinHue must be between 0 and 359.");
			}
			else if (maxHue < 0 || maxHue > 359)
			{
				throw new ArgumentException("MaxHue must be between 0 and 359.");
			}

			m_currentHsv.H = Math.Max((double)minHue, Math.Min(m_currentHsv.H, (double)maxHue));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnMinMaxSaturationChanged(DependencyPropertyChangedEventArgs args)
		{
			int minSaturation = this.MinSaturation;
			int maxSaturation = this.MaxSaturation;

			if (minSaturation < 0 || minSaturation > 100)
			{
				throw new ArgumentException("MinSaturation must be between 0 and 100.");
			}
			else if (maxSaturation < 0 || maxSaturation > 100)
			{
				throw new ArgumentException("MaxSaturation must be between 0 and 100.");
			}

			m_currentHsv.S = Math.Max((double)minSaturation / 100, Math.Min(m_currentHsv.S, (double)maxSaturation / 100));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnMinMaxValueChanged(DependencyPropertyChangedEventArgs args)
		{
			int minValue = this.MinValue;
			int maxValue = this.MaxValue;

			if (minValue < 0 || minValue > 100)
			{
				throw new ArgumentException("MinValue must be between 0 and 100.");
			}
			else if (maxValue < 0 || maxValue > 100)
			{
				throw new ArgumentException("MaxValue must be between 0 and 100.");
			}

			m_currentHsv.V = Math.Max((double)minValue / 100, Math.Min(m_currentHsv.V, (double)maxValue / 100));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnColorSpectrumComponentsChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateThirdDimensionSlider();
			SetThirdDimensionSliderChannel();
		}

		private void OnOrientationChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualState(true);
		}

		private new void UpdateVisualState(bool useTransitions)
		{
			Color? previousColor = this.PreviousColor;
			bool isAlphaEnabled = this.IsAlphaEnabled;
			bool isColorSpectrumVisible = this.IsColorSpectrumVisible;
			bool isVerticalOrientation = this.Orientation == Orientation.Vertical;

			string previousColorStateName;

			if (isColorSpectrumVisible)
			{
				previousColorStateName = previousColor.HasValue ? "PreviousColorVisibleVertical" : "PreviousColorCollapsedVertical";
			}
			else
			{
				previousColorStateName = previousColor.HasValue ? "PreviousColorVisibleHorizontal" : "PreviousColorCollapsedHorizontal";
			}

			VisualStateManager.GoToState(this, isColorSpectrumVisible ? "ColorSpectrumVisible" : "ColorSpectrumCollapsed", useTransitions);
			VisualStateManager.GoToState(this, previousColorStateName, useTransitions);
			VisualStateManager.GoToState(this, this.IsColorPreviewVisible ? "ColorPreviewVisible" : "ColorPreviewCollapsed", useTransitions);
			VisualStateManager.GoToState(this, this.IsColorSliderVisible ? "ThirdDimensionSliderVisible" : "ThirdDimensionSliderCollapsed", useTransitions);
			VisualStateManager.GoToState(this, isAlphaEnabled && this.IsAlphaSliderVisible ? "AlphaSliderVisible" : "AlphaSliderCollapsed", useTransitions);
			// More button is disabled in horizontal orientation; only respect IsMoreButtonVisible states when switching to Vertical orientation.
			VisualStateManager.GoToState(this, this.IsMoreButtonVisible && isVerticalOrientation ? "MoreButtonVisible" : "MoreButtonCollapsed", useTransitions);
			VisualStateManager.GoToState(this, !this.IsMoreButtonVisible || m_textEntryGridOpened || !isVerticalOrientation ? "TextEntryGridVisible" : "TextEntryGridCollapsed", useTransitions);

			if (m_colorRepresentationComboBox is ComboBox colorRepresentationComboBox)
			{
				VisualStateManager.GoToState(this, colorRepresentationComboBox.SelectedIndex == 0 ? "RgbSelected" : "HsvSelected", useTransitions);
			}

			VisualStateManager.GoToState(this, this.IsColorChannelTextInputVisible ? "ColorChannelTextInputVisible" : "ColorChannelTextInputCollapsed", useTransitions);
			VisualStateManager.GoToState(this, isAlphaEnabled && this.IsAlphaTextInputVisible ? "AlphaTextInputVisible" : "AlphaTextInputCollapsed", useTransitions);
			VisualStateManager.GoToState(this, this.IsHexInputVisible ? "HexInputVisible" : "HexInputCollapsed", useTransitions);
			VisualStateManager.GoToState(this, isAlphaEnabled ? "AlphaEnabled" : "AlphaDisabled", useTransitions);
			VisualStateManager.GoToState(this, isVerticalOrientation ? "Vertical" : "Horizontal", useTransitions);
		}

		private void InitializeColor()
		{
			var color = this.Color;

			m_currentRgb = new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0);
			m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
			m_currentAlpha = color.A / 255.0;
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(ColorUpdateReason.InitializingColor);
		}

		private void UpdateColor(Rgb rgb, ColorUpdateReason reason)
		{
			m_currentRgb = rgb;
			m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void UpdateColor(Hsv hsv, ColorUpdateReason reason)
		{
			m_currentHsv = hsv;
			m_currentRgb = ColorConversion.HsvToRgb(hsv);
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void UpdateColor(double alpha, ColorUpdateReason reason)
		{
			m_currentAlpha = alpha;
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void SetColorAndUpdateControls(ColorUpdateReason reason)
		{
			m_updatingColor = true;

			Color = ColorConversion.ColorFromRgba(m_currentRgb, m_currentAlpha);
			UpdateColorControls(reason);

			m_updatingColor = false;
		}

		private void UpdatePreviousColorRectangle()
		{
			if (m_previousColorRectangle is Rectangle previousColorRectangle)
			{
				Color? previousColor = this.PreviousColor;

				if (previousColor.HasValue)
				{
					previousColorRectangle.Fill = new SolidColorBrush(previousColor.Value);
				}
				else
				{
					previousColorRectangle.Fill = null;
				}
			}
		}

		private void UpdateColorControls(ColorUpdateReason reason)
		{
			// If we're updating the controls internally, we don't want to execute any of the controls'
			// event handlers, because that would then update the color, which would update the color controls,
			// and then we'd be in an infinite loop.
			m_updatingControls = true;

			// We pass in the reason why we're updating the color controls because
			// we don't want to re-update any control that was the cause of this update.
			// For example, if a user selected a color on the ColorSpectrum, then we
			// don't want to update the ColorSpectrum's color based on this change.
			if (reason != ColorUpdateReason.ColorSpectrumColorChanged && m_colorSpectrum != null)
			{
				m_colorSpectrum.HsvColor = new Vector4((float)m_currentHsv.H, (float)m_currentHsv.S, (float)m_currentHsv.V, (float)m_currentAlpha);
			}

			if (m_colorPreviewRectangle is Rectangle colorPreviewRectangle)
			{
				var color = this.Color;

				colorPreviewRectangle.Fill = new SolidColorBrush(color);
			}

			if (reason != ColorUpdateReason.ThirdDimensionSliderChanged && m_thirdDimensionSlider != null)
			{
				UpdateThirdDimensionSlider();
			}

			if (reason != ColorUpdateReason.AlphaSliderChanged && m_alphaSlider != null)
			{
				UpdateAlphaSlider();
			}

			var strongThis = this;
			Action updateTextBoxes = () =>
			{
				if (reason != ColorUpdateReason.RgbTextBoxChanged)
				{
					if (strongThis.m_redTextBox is TextBox redTextBox)
					{
						redTextBox.Text = ((byte)Math.Round(strongThis.m_currentRgb.R * 255)).ToString(CultureInfo.InvariantCulture);
					}

					if (strongThis.m_greenTextBox is TextBox greenTextBox)
					{
						greenTextBox.Text = ((byte)Math.Round(strongThis.m_currentRgb.G * 255)).ToString(CultureInfo.InvariantCulture);
					}

					if (strongThis.m_blueTextBox is TextBox blueTextBox)
					{
						blueTextBox.Text = ((byte)Math.Round(strongThis.m_currentRgb.B * 255)).ToString(CultureInfo.InvariantCulture);
					}
				}

				if (reason != ColorUpdateReason.HsvTextBoxChanged)
				{
					if (strongThis.m_hueTextBox is TextBox hueTextBox)
					{
						hueTextBox.Text = ((int)Math.Round(strongThis.m_currentHsv.H)).ToString(CultureInfo.InvariantCulture);
					}

					if (strongThis.m_saturationTextBox is TextBox saturationTextBox)
					{
						saturationTextBox.Text = ((int)Math.Round(strongThis.m_currentHsv.S * 100)).ToString(CultureInfo.InvariantCulture);
					}

					if (strongThis.m_valueTextBox is TextBox valueTextBox)
					{
						valueTextBox.Text = ((int)Math.Round(strongThis.m_currentHsv.V * 100)).ToString(CultureInfo.InvariantCulture);
					}
				}

				if (reason != ColorUpdateReason.AlphaTextBoxChanged)
				{
					if (strongThis.m_alphaTextBox is TextBox alphaTextBox)
					{
						alphaTextBox.Text = ((int)Math.Round(strongThis.m_currentAlpha * 100)).ToString(CultureInfo.InvariantCulture) + "%";
					}
				}

				if (reason != ColorUpdateReason.HexTextBoxChanged)
				{
					if (strongThis.m_hexTextBox is TextBox hexTextBox)
					{
						hexTextBox.Text = strongThis.m_currentHex;
					}
				}
			};

			if (SharedHelpers.IsRS2OrHigher())
			{
				// A reentrancy bug with setting TextBox.Text was fixed in RS2,
				// so we can just directly set the TextBoxes' Text property there.
				updateTextBoxes();
			}
			else if (!SharedHelpers.IsInDesignMode())
			{
				// Otherwise, we need to post this to the dispatcher to avoid that reentrancy bug.
				// Uno Doc: Assumed normal priority is acceptable
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					strongThis.m_updatingControls = true;
					updateTextBoxes();
					strongThis.m_updatingControls = false;
				});
			}

			m_updatingControls = false;
		}

		private void OnColorSpectrumColorChanged(Primitives.ColorSpectrum sender, ColorChangedEventArgs args)
		{
			// If we're updating controls, then this is being raised in response to that,
			// so we'll ignore it.
			if (m_updatingControls)
			{
				return;
			}

			var hsvColor = sender.HsvColor;
			UpdateColor(new Hsv(Hsv.GetHue(hsvColor), Hsv.GetSaturation(hsvColor), Hsv.GetValue(hsvColor)), ColorUpdateReason.ColorSpectrumColorChanged);
		}

		private void OnColorSpectrumSizeChanged(object sender, SizeChangedEventArgs args)
		{
			// Since the ColorPicker is arranged vertically, the ColorSpectrum's height can be whatever we want it to be -
			// the width is the limiting factor.  Since we want it to always be a square, we'll set its height to whatever its width is.
			if (args.NewSize.Width != args.PreviousSize.Width)
			{
				m_colorSpectrum.Height = args.NewSize.Width;
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			// Uno Doc: Added to re-register disposed event handlers
			if (_isTemplateApplied && _eventSubscriptions.Disposable == null)
			{
				_eventSubscriptions.Disposable = SubscribeToEvents();
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			// If we're in the middle of creating image bitmaps while being unloaded,
			// we'll want to synchronously cancel it so we don't have any asynchronous actions
			// lingering beyond our lifetime.
			ColorHelpers.CancelAsyncAction(m_createColorPreviewRectangleCheckeredBackgroundBitmapAction);
			ColorHelpers.CancelAsyncAction(m_alphaSliderCheckeredBackgroundBitmapAction);

			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = null;
		}

		private void OnCheckerColorChanged(DependencyObject o, DependencyProperty p)
		{
			CreateColorPreviewCheckeredBackground();
			CreateAlphaSliderCheckeredBackground();
		}

		private void OnColorPreviewRectangleGridSizeChanged(object sender, SizeChangedEventArgs args)
		{
			CreateColorPreviewCheckeredBackground();
		}

		private void OnAlphaSliderBackgroundRectangleSizeChanged(object sender, SizeChangedEventArgs args)
		{
			CreateAlphaSliderCheckeredBackground();
		}

		private void OnThirdDimensionSliderValueChanged(object sender, RangeBaseValueChangedEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			ColorSpectrumComponents components = this.ColorSpectrumComponents;

			double h = m_currentHsv.H;
			double s = m_currentHsv.S;
			double v = m_currentHsv.V;

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					s = m_thirdDimensionSlider.Value / 100.0;
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					v = m_thirdDimensionSlider.Value / 100.0;
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					h = m_thirdDimensionSlider.Value;
					break;
			}

			UpdateColor(new Hsv(h, s, v), ColorUpdateReason.ThirdDimensionSliderChanged);
		}

		private void OnAlphaSliderValueChanged(object sender, RangeBaseValueChangedEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			UpdateColor(m_alphaSlider.Value / 100.0, ColorUpdateReason.AlphaSliderChanged);
		}

		private void OnMoreButtonClicked(object sender, RoutedEventArgs args)
		{
			m_textEntryGridOpened = !m_textEntryGridOpened;
			UpdateMoreButton();
		}

		private void OnMoreButtonChecked(object sender, RoutedEventArgs args)
		{
			m_textEntryGridOpened = true;
			UpdateMoreButton();
		}

		private void OnMoreButtonUnchecked(object sender, RoutedEventArgs args)
		{
			m_textEntryGridOpened = false;
			UpdateMoreButton();
		}

		private void UpdateMoreButton()
		{
			if (m_moreButton is ButtonBase moreButton)
			{
				AutomationProperties.SetName(moreButton, ResourceAccessor.GetLocalizedStringResource(m_textEntryGridOpened ? ResourceAccessor.SR_AutomationNameMoreButtonExpanded : ResourceAccessor.SR_AutomationNameMoreButtonCollapsed));
			}

			if (m_moreButtonLabel is TextBlock moreButtonLabel)
			{
				moreButtonLabel.Text = ResourceAccessor.GetLocalizedStringResource(m_textEntryGridOpened ? ResourceAccessor.SR_TextMoreButtonLabelExpanded : ResourceAccessor.SR_TextMoreButtonLabelCollapsed);
			}

			UpdateVisualState(useTransitions: true);
		}

		private void OnColorRepresentationComboBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			UpdateVisualState(useTransitions: true);
		}

		private void OnTextBoxGotFocus(object sender, RoutedEventArgs args)
		{
			TextBox textBox = sender as TextBox;

			m_isFocusedTextBoxValid = true;
			m_previousString = textBox.Text;
		}

		private void OnTextBoxLostFocus(object sender, RoutedEventArgs args)
		{
			TextBox textBox = sender as TextBox;

			// When a text box loses focus, we want to check whether its contents were valid.
			// If they weren't, then we'll roll back its contents to their last valid value.
			if (!m_isFocusedTextBoxValid)
			{
				textBox.Text = m_previousString;
			}

			// Now that we know that no text box is currently being edited, we'll update all of the color controls
			// in order to clear away any invalid values currently in any text box.
			UpdateColorControls(ColorUpdateReason.ColorPropertyChanged);
		}

		private void OnRgbTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var componentValue = ColorConversion.TryParseInt(sender.Text);
			if (!componentValue.HasValue ||
				componentValue.Value < 0 ||
				componentValue.Value > 255)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(ApplyConstraintsToRgbColor(GetRgbColorFromTextBoxes()), ColorUpdateReason.RgbTextBoxChanged);
			}
		}

		private void OnHueTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var hueValue = ColorConversion.TryParseInt(m_hueTextBox.Text);
			if (!hueValue.HasValue ||
				hueValue.Value < (ulong)this.MinHue ||
				hueValue.Value > (ulong)this.MaxHue)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnSaturationTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var saturationValue = ColorConversion.TryParseInt(m_saturationTextBox.Text);
			if (!saturationValue.HasValue ||
				saturationValue.Value < (ulong)this.MinSaturation ||
				saturationValue.Value > (ulong)this.MaxSaturation)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnValueTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var value = ColorConversion.TryParseInt(m_valueTextBox.Text);
			if (!value.HasValue ||
				value.Value < (ulong)this.MinValue ||
				value.Value > (ulong)this.MaxValue)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnAlphaTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			if (m_alphaTextBox is TextBox alphaTextBox)
			{
				// If the user hasn't entered a %, we'll do that for them, keeping the cursor
				// where it was before.
				int cursorPosition = alphaTextBox.SelectionStart + alphaTextBox.SelectionLength;

				var text = alphaTextBox.Text;
				if (string.IsNullOrEmpty(text) || text[text.Length - 1] != '%')
				{
					alphaTextBox.Text = text + "%";
					alphaTextBox.SelectionStart = cursorPosition;
				}

				// We'll respond to the text change if the user has entered a valid value.
				// Otherwise, we'll do nothing except mark the text box's contents as invalid.
				string alphaString = alphaTextBox.Text.Substring(0, alphaTextBox.Text.Length - 1);
				var alphaValue = ColorConversion.TryParseInt(alphaString);
				if (!alphaValue.HasValue || alphaValue.Value < 0 || alphaValue.Value > 100)
				{
					m_isFocusedTextBoxValid = false;
				}
				else
				{
					m_isFocusedTextBoxValid = true;
					UpdateColor(alphaValue.Value / 100.0, ColorUpdateReason.AlphaTextBoxChanged);
				}
			}
		}

		private void OnHexTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			var hexTextBox = m_hexTextBox;

			// If the user hasn't entered a #, we'll do that for them, keeping the cursor
			// where it was before.
			var text = hexTextBox.Text;
			if (string.IsNullOrEmpty(text) || text[0] != '#')
			{
				hexTextBox.Text = "#" + text;
				hexTextBox.SelectionStart = hexTextBox.Text.Length;
			}

			// Uno Doc: This section assigning rgbValue/alphaValue was re-written in C# to avoid strange usage of functions in C++
			//const bool isAlphaEnabled = IsAlphaEnabled();
			//auto [rgbValue, alphaValue] = [this, isAlphaEnabled]() {
			//    return isAlphaEnabled ?
			//        HexToRgba(m_hexTextBox.get().Text()) :
			//        std::make_tuple(HexToRgb(m_hexTextBox.get().Text()), 1.0);
			//}();

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			Rgb rgbValue;
			double alphaValue;
			if (this.IsAlphaEnabled)
			{
				(rgbValue, alphaValue) = ColorConversion.HexToRgba(m_hexTextBox.Text);
			}
			else
			{
				rgbValue = ColorConversion.HexToRgb(m_hexTextBox.Text);
				alphaValue = 1.0;
			}

			if ((rgbValue.R == -1 && rgbValue.G == -1 && rgbValue.B == -1 && alphaValue == -1) || alphaValue < 0 || alphaValue > 1)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(ApplyConstraintsToRgbColor(rgbValue), ColorUpdateReason.HexTextBoxChanged);
				UpdateColor(alphaValue, ColorUpdateReason.HexTextBoxChanged);
			}
		}

		private Rgb GetRgbColorFromTextBoxes()
		{
			// Uno Doc: There is no drop-in C# equivalent for the C++ '_wtoi' function; therefore, this code is re-written.
			_ = int.TryParse(m_redTextBox?.Text, CultureInfo.InvariantCulture, out int redValue);
			_ = int.TryParse(m_greenTextBox?.Text, CultureInfo.InvariantCulture, out int greenValue);
			_ = int.TryParse(m_blueTextBox?.Text, CultureInfo.InvariantCulture, out int blueValue);

			return new Rgb(redValue / 255.0, greenValue / 255.0, blueValue / 255.0);
		}

		private Hsv GetHsvColorFromTextBoxes()
		{
			// Uno Doc: There is no drop-in C# equivalent for the C++ '_wtoi' function; therefore, this code is re-written.
			_ = int.TryParse(m_hueTextBox?.Text, CultureInfo.InvariantCulture, out int hueValue);
			_ = int.TryParse(m_saturationTextBox?.Text, CultureInfo.InvariantCulture, out int saturationValue);
			_ = int.TryParse(m_valueTextBox?.Text, CultureInfo.InvariantCulture, out int valueValue);

			return new Hsv(hueValue, saturationValue / 100.0, valueValue / 100.0);
		}

		private string GetCurrentHexValue()
		{
			return this.IsAlphaEnabled ? ColorConversion.RgbaToHex(m_currentRgb, m_currentAlpha) : ColorConversion.RgbToHex(m_currentRgb);
		}

		private Rgb ApplyConstraintsToRgbColor(Rgb rgb)
		{
			double minHue = this.MinHue;
			double maxHue = this.MaxHue;
			double minSaturation = this.MinSaturation / 100.0;
			double maxSaturation = this.MaxSaturation / 100.0;
			double minValue = this.MinValue / 100.0;
			double maxValue = this.MaxValue / 100.0;

			Hsv hsv = ColorConversion.RgbToHsv(rgb);

			hsv.H = Math.Min(Math.Max(hsv.H, minHue), maxHue);
			hsv.S = Math.Min(Math.Max(hsv.S, minSaturation), maxSaturation);
			hsv.V = Math.Min(Math.Max(hsv.V, minValue), maxValue);

			return ColorConversion.HsvToRgb(hsv);
		}

		private CompositeDisposable SubscribeToEvents()
		{
			var registrations = new CompositeDisposable();

			if (m_colorSpectrum is Primitives.ColorSpectrum colorSpectrum)
			{
				colorSpectrum.ColorChanged += OnColorSpectrumColorChanged;
				colorSpectrum.SizeChanged += OnColorSpectrumSizeChanged;

				registrations.Add(() => colorSpectrum.ColorChanged -= OnColorSpectrumColorChanged);
				registrations.Add(() => colorSpectrum.SizeChanged -= OnColorSpectrumSizeChanged);
			}

			if (m_colorPreviewRectangleGrid is Grid colorPreviewRectangleGrid)
			{
				colorPreviewRectangleGrid.SizeChanged += OnColorPreviewRectangleGridSizeChanged;
				registrations.Add(() => colorPreviewRectangleGrid.SizeChanged -= OnColorPreviewRectangleGridSizeChanged);
			}

			if (m_thirdDimensionSlider is Primitives.ColorPickerSlider thirdDimensionSlider)
			{
				thirdDimensionSlider.ValueChanged += OnThirdDimensionSliderValueChanged;
				registrations.Add(() => thirdDimensionSlider.ValueChanged -= OnThirdDimensionSliderValueChanged);
			}

			if (m_alphaSlider is Primitives.ColorPickerSlider alphaSlider)
			{
				alphaSlider.ValueChanged += OnAlphaSliderValueChanged;
				registrations.Add(() => alphaSlider.ValueChanged -= OnAlphaSliderValueChanged);
			}

			if (m_alphaSliderBackgroundRectangle is Rectangle alphaSliderBackgroundRectangle)
			{
				alphaSliderBackgroundRectangle.SizeChanged += OnAlphaSliderBackgroundRectangleSizeChanged;
				registrations.Add(() => alphaSliderBackgroundRectangle.SizeChanged -= OnAlphaSliderBackgroundRectangleSizeChanged);
			}

			if (m_moreButton is ButtonBase moreButton)
			{
				if (moreButton is ToggleButton moreButtonAsToggleButton)
				{
					moreButtonAsToggleButton.Checked += OnMoreButtonChecked;
					moreButtonAsToggleButton.Unchecked += OnMoreButtonUnchecked;
					registrations.Add(() => moreButtonAsToggleButton.Checked -= OnMoreButtonChecked);
					registrations.Add(() => moreButtonAsToggleButton.Unchecked -= OnMoreButtonUnchecked);
				}
				else
				{
					moreButton.Click += OnMoreButtonClicked;
					registrations.Add(() => moreButton.Click -= OnMoreButtonClicked);
				}
			}

			if (m_colorRepresentationComboBox is ComboBox colorRepresentationComboBox)
			{
				colorRepresentationComboBox.SelectionChanged += OnColorRepresentationComboBoxSelectionChanged;
				registrations.Add(() => colorRepresentationComboBox.SelectionChanged -= OnColorRepresentationComboBoxSelectionChanged);
			}

			if (m_redTextBox is TextBox redTextBox)
			{
				redTextBox.TextChanging += OnRgbTextChanging;
				redTextBox.GotFocus += OnTextBoxGotFocus;
				redTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => redTextBox.TextChanging -= OnRgbTextChanging);
				registrations.Add(() => redTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => redTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_greenTextBox is TextBox greenTextBox)
			{
				greenTextBox.TextChanging += OnRgbTextChanging;
				greenTextBox.GotFocus += OnTextBoxGotFocus;
				greenTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => greenTextBox.TextChanging -= OnRgbTextChanging);
				registrations.Add(() => greenTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => greenTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_blueTextBox is TextBox blueTextBox)
			{
				blueTextBox.TextChanging += OnRgbTextChanging;
				blueTextBox.GotFocus += OnTextBoxGotFocus;
				blueTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => blueTextBox.TextChanging -= OnRgbTextChanging);
				registrations.Add(() => blueTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => blueTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_hueTextBox is TextBox hueTextBox)
			{
				hueTextBox.TextChanging += OnHueTextChanging;
				hueTextBox.GotFocus += OnTextBoxGotFocus;
				hueTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => hueTextBox.TextChanging -= OnHueTextChanging);
				registrations.Add(() => hueTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => hueTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_saturationTextBox is TextBox saturationTextBox)
			{
				saturationTextBox.TextChanging += OnSaturationTextChanging;
				saturationTextBox.GotFocus += OnTextBoxGotFocus;
				saturationTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => saturationTextBox.TextChanging -= OnSaturationTextChanging);
				registrations.Add(() => saturationTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => saturationTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_valueTextBox is TextBox valueTextBox)
			{
				valueTextBox.TextChanging += OnValueTextChanging;
				valueTextBox.GotFocus += OnTextBoxGotFocus;
				valueTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => valueTextBox.TextChanging -= OnValueTextChanging);
				registrations.Add(() => valueTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => valueTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_alphaTextBox is TextBox alphaTextBox)
			{
				alphaTextBox.TextChanging += OnAlphaTextChanging;
				alphaTextBox.GotFocus += OnTextBoxGotFocus;
				alphaTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => alphaTextBox.TextChanging -= OnAlphaTextChanging);
				registrations.Add(() => alphaTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => alphaTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			if (m_hexTextBox is TextBox hexTextBox)
			{
				hexTextBox.TextChanging += OnHexTextChanging;
				hexTextBox.GotFocus += OnTextBoxGotFocus;
				hexTextBox.LostFocus += OnTextBoxLostFocus;

				registrations.Add(() => hexTextBox.TextChanging -= OnHexTextChanging);
				registrations.Add(() => hexTextBox.GotFocus -= OnTextBoxGotFocus);
				registrations.Add(() => hexTextBox.LostFocus -= OnTextBoxLostFocus);
			}

			return registrations;
		}

		private void UpdateThirdDimensionSlider()
		{
			if (m_thirdDimensionSlider == null || m_thirdDimensionSliderGradientBrush == null)
			{
				return;
			}

			var thirdDimensionSlider = m_thirdDimensionSlider;
			var thirdDimensionSliderGradientBrush = m_thirdDimensionSliderGradientBrush;

			// Since the slider changes only one color dimension, we can use a LinearGradientBrush
			// for its background instead of needing to manually set pixels ourselves.
			// We'll have the gradient go between the minimum and maximum values in the case where
			// the slider handles saturation or value, or in the case where it handles hue,
			// we'll have it go between red, yellow, green, cyan, blue, and purple, in that order.
			thirdDimensionSliderGradientBrush.GradientStops.Clear();

			switch (this.ColorSpectrumComponents)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					{
						int minSaturation = this.MinSaturation;
						int maxSaturation = this.MaxSaturation;

						thirdDimensionSlider.Minimum = minSaturation;
						thirdDimensionSlider.Maximum = maxSaturation;
						thirdDimensionSlider.Value = m_currentHsv.S * 100;

						// If MinSaturation >= MaxSaturation, then by convention MinSaturation is the only value
						// that the slider can take.
						if (minSaturation >= maxSaturation)
						{
							maxSaturation = minSaturation;
						}

						AddGradientStop(thirdDimensionSliderGradientBrush, 0.0, new Hsv(m_currentHsv.H, minSaturation / 100.0, 1.0), 1.0);
						AddGradientStop(thirdDimensionSliderGradientBrush, 1.0, new Hsv(m_currentHsv.H, maxSaturation / 100.0, 1.0), 1.0);
					}
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					{
						int minValue = this.MinValue;
						int maxValue = this.MaxValue;

						thirdDimensionSlider.Minimum = minValue;
						thirdDimensionSlider.Maximum = maxValue;
						thirdDimensionSlider.Value = m_currentHsv.V * 100;

						// If MinValue >= MaxValue, then by convention MinValue is the only value
						// that the slider can take.
						if (minValue >= maxValue)
						{
							maxValue = minValue;
						}

						AddGradientStop(thirdDimensionSliderGradientBrush, 0.0, new Hsv(m_currentHsv.H, m_currentHsv.S, minValue / 100.0), 1.0);
						AddGradientStop(thirdDimensionSliderGradientBrush, 1.0, new Hsv(m_currentHsv.H, m_currentHsv.S, maxValue / 100.0), 1.0);
					}
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					{
						int minHue = this.MinHue;
						int maxHue = this.MaxHue;

						thirdDimensionSlider.Minimum = minHue;
						thirdDimensionSlider.Maximum = maxHue;
						thirdDimensionSlider.Value = m_currentHsv.H;

						// If MinHue >= MaxHue, then by convention MinHue is the only value
						// that the slider can take.
						if (minHue >= maxHue)
						{
							maxHue = minHue;
						}

						double minOffset = minHue / 359.0;
						double maxOffset = maxHue / 359.0;

						// With unclamped hue values, we have six different gradient stops, corresponding to red, yellow, green, cyan, blue, and purple.
						// However, with clamped hue values, we may not need all of those gradient stops.
						// We know we need a gradient stop at the start and end corresponding to the min and max values for hue,
						// and then in the middle, we'll add any gradient stops corresponding to the hue of those six pure colors that exist
						// between the min and max hue.
						AddGradientStop(thirdDimensionSliderGradientBrush, 0.0, new Hsv((double)minHue, 1.0, 1.0), 1.0);

						for (int sextant = 1; sextant <= 5; sextant++)
						{
							double offset = sextant / 6.0;

							if (minOffset < offset && maxOffset > offset)
							{
								AddGradientStop(thirdDimensionSliderGradientBrush, (offset - minOffset) / (maxOffset - minOffset), new Hsv(60.0 * sextant, 1.0, 1.0), 1.0);
							}
						}

						AddGradientStop(thirdDimensionSliderGradientBrush, 1.0, new Hsv((double)maxHue, 1.0, 1.0), 1.0);
					}
					break;
			}
		}

		private void SetThirdDimensionSliderChannel()
		{
			if (m_thirdDimensionSlider is Primitives.ColorPickerSlider thirdDimensionSlider)
			{
				switch (this.ColorSpectrumComponents)
				{
					case ColorSpectrumComponents.ValueSaturation:
					case ColorSpectrumComponents.SaturationValue:
						thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Hue;
						AutomationProperties.SetName(thirdDimensionSlider, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameHueSlider));
						break;

					case ColorSpectrumComponents.HueValue:
					case ColorSpectrumComponents.ValueHue:
						thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Saturation;
						AutomationProperties.SetName(thirdDimensionSlider, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameSaturationSlider));
						break;

					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.SaturationHue:
						thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Value;
						AutomationProperties.SetName(thirdDimensionSlider, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameValueSlider));
						break;
				}
			}
		}

		private void UpdateAlphaSlider()
		{
			if (m_alphaSlider == null || m_alphaSliderGradientBrush == null)
			{
				return;
			}

			var alphaSlider = m_alphaSlider;
			var alphaSliderGradientBrush = m_alphaSliderGradientBrush;

			// Since the slider changes only one color dimension, we can use a LinearGradientBrush
			// for its background instead of needing to manually set pixels ourselves.
			// We'll have the gradient go between the minimum and maximum values in the case where
			// the slider handles saturation or value, or in the case where it handles hue,
			// we'll have it go between red, yellow, green, cyan, blue, and purple, in that order.
			alphaSliderGradientBrush.GradientStops.Clear();

			alphaSlider.Minimum = 0;
			alphaSlider.Maximum = 100;
			alphaSlider.Value = m_currentAlpha * 100;

			AddGradientStop(alphaSliderGradientBrush, 0.0, m_currentHsv, 0.0);
			AddGradientStop(alphaSliderGradientBrush, 1.0, m_currentHsv, 1.0);
		}

		private void CreateColorPreviewCheckeredBackground()
		{
			if (SharedHelpers.IsInDesignMode()) { return; }

			if (m_colorPreviewRectangleGrid is Grid colorPreviewRectangleGrid)
			{
				if (m_colorPreviewRectangleCheckeredBackgroundImageBrush != null)
				{
					int width = (int)Math.Round(colorPreviewRectangleGrid.ActualWidth);
					int height = (int)Math.Round(colorPreviewRectangleGrid.ActualHeight);
					ArrayList<byte> bgraCheckeredPixelData = new ArrayList<byte>();
					var strongThis = this;

					ColorHelpers.CreateCheckeredBackgroundAsync(
						width,
						height,
						GetCheckerColor(),
						bgraCheckeredPixelData,
						m_createColorPreviewRectangleCheckeredBackgroundBitmapAction,
						this.Dispatcher,
						(WriteableBitmap checkeredBackgroundSoftwareBitmap) =>
						{
							strongThis.m_colorPreviewRectangleCheckeredBackgroundImageBrush.ImageSource = checkeredBackgroundSoftwareBitmap;
						});
				}
			}
		}

		private void CreateAlphaSliderCheckeredBackground()
		{
			if (SharedHelpers.IsInDesignMode()) { return; }

			if (m_alphaSliderBackgroundRectangle is Rectangle alphaSliderBackgroundRectangle)
			{
				if (m_alphaSliderCheckeredBackgroundImageBrush != null)
				{
					int width = (int)Math.Round(alphaSliderBackgroundRectangle.ActualWidth);
					int height = (int)Math.Round(alphaSliderBackgroundRectangle.ActualHeight);
					ArrayList<byte> bgraCheckeredPixelData = new ArrayList<byte>();
					var strongThis = this;

					ColorHelpers.CreateCheckeredBackgroundAsync(
						width,
						height,
						GetCheckerColor(),
						bgraCheckeredPixelData,
						m_alphaSliderCheckeredBackgroundBitmapAction,
						this.Dispatcher,
						(WriteableBitmap checkeredBackgroundSoftwareBitmap) =>
						{
							strongThis.m_alphaSliderCheckeredBackgroundImageBrush.ImageSource = checkeredBackgroundSoftwareBitmap;
						});
				}
			}
		}

		private void AddGradientStop(LinearGradientBrush brush, double offset, Hsv hsvColor, double alpha)
		{
			GradientStop stop = new GradientStop();

			Rgb rgbColor = ColorConversion.HsvToRgb(hsvColor);

			stop.Color = ColorHelper.FromArgb(
				(byte)Math.Round(alpha * 255),
				(byte)Math.Round(rgbColor.R * 255),
				(byte)Math.Round(rgbColor.G * 255),
				(byte)Math.Round(rgbColor.B * 255));
			stop.Offset = offset;

			brush.GradientStops.Add(stop);
		}

		private Color GetCheckerColor()
		{
			Color checkerColor;

			if (m_checkerColorBrush is SolidColorBrush checkerColorBrush)
			{
				checkerColor = checkerColorBrush.Color;
			}
			else
			{
				object checkerColorAsI = Application.Current.Resources["SystemListLowColor"];
				checkerColor = (Color)checkerColorAsI;
			}

			return checkerColor;
		}
	}
}
