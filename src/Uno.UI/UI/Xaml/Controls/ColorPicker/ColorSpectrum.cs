using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Core;
using Uno.UI.Xaml.Core;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ColorSpectrum : Control
	{
		private bool m_updatingColor;
		private bool m_updatingHsvColor;
		private bool m_isPointerOver;
		private bool m_isPointerPressed;
		private bool m_shouldShowLargeSelection;
		private List<Hsv> m_hsvValues = new List<Hsv>(); // Uno Doc: Set an instance unlike WinUI

		// XAML elements
		private Grid m_layoutRoot;
		private Grid m_sizingGrid;

		private Rectangle m_spectrumRectangle;
		private Ellipse m_spectrumEllipse;
		private Rectangle m_spectrumOverlayRectangle;
		private Ellipse m_spectrumOverlayEllipse;

		private FrameworkElement m_inputTarget;
		private Panel m_selectionEllipsePanel;

		private ToolTip m_colorNameToolTip;

		private IAsyncAction m_createImageBitmapAction;

		// On RS1 and before, we put the spectrum images in a bitmap,
		// which we then give to an ImageBrush.
		private WriteableBitmap m_hueRedBitmap;
		private WriteableBitmap m_hueYellowBitmap;
		private WriteableBitmap m_hueGreenBitmap;
		private WriteableBitmap m_hueCyanBitmap;
		private WriteableBitmap m_hueBlueBitmap;
		private WriteableBitmap m_huePurpleBitmap;

		private WriteableBitmap m_saturationMinimumBitmap;
		private WriteableBitmap m_saturationMaximumBitmap;

		private WriteableBitmap m_valueBitmap;

		// On RS2 and later, we put the spectrum images in a loaded image surface,
		// which we then put into a SpectrumBrush.
		// Uno Doc: SpectrumBrush is disabled for now
		//private LoadedImageSurface m_hueRedSurface;
		//private LoadedImageSurface m_hueYellowSurface;
		//private LoadedImageSurface m_hueGreenSurface;
		//private LoadedImageSurface m_hueCyanSurface;
		//private LoadedImageSurface m_hueBlueSurface;
		//private LoadedImageSurface m_huePurpleSurface;

		//private LoadedImageSurface m_saturationMinimumSurface;
		//private LoadedImageSurface m_saturationMaximumSurface;

		//private LoadedImageSurface m_valueSurface;

		// Fields used by UpdateEllipse() to ensure that it's using the data
		// associated with the last call to CreateBitmapsAndColorMap(),
		// in order to function properly while the asynchronous bitmap creation
		// is in progress.
		private ColorSpectrumShape m_shapeFromLastBitmapCreation = ColorSpectrumShape.Box;
		private ColorSpectrumComponents m_componentsFromLastBitmapCreation = ColorSpectrumComponents.HueSaturation;
		private double m_imageWidthFromLastBitmapCreation = 0.0;
		private double m_imageHeightFromLastBitmapCreation = 0.0;
		private int m_minHueFromLastBitmapCreation = 0;
		private int m_maxHueFromLastBitmapCreation = 0;
		private int m_minSaturationFromLastBitmapCreation = 0;
		private int m_maxSaturationFromLastBitmapCreation = 0;
		private int m_minValueFromLastBitmapCreation = 0;
		private int m_maxValueFromLastBitmapCreation = 0;

		private Color m_oldColor = Color.FromArgb(255, 255, 255, 255);
		private Vector4 m_oldHsvColor = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);

		// Uno Doc: Added to dispose event handlers
		private bool _isTemplateApplied = false;
		private SerialDisposable _eventSubscriptions = new SerialDisposable();

		public ColorSpectrum()
		{
			this.SetDefaultStyleKey();

			m_updatingColor = false;
			m_updatingHsvColor = false;
			m_isPointerOver = false;
			m_isPointerPressed = false;
			m_shouldShowLargeSelection = false;

			m_shapeFromLastBitmapCreation = this.Shape;
			m_componentsFromLastBitmapCreation = this.Components;
			m_imageWidthFromLastBitmapCreation = 0;
			m_imageHeightFromLastBitmapCreation = 0;
			m_minHueFromLastBitmapCreation = this.MinHue;
			m_maxHueFromLastBitmapCreation = this.MaxHue;
			m_minSaturationFromLastBitmapCreation = this.MinSaturation;
			m_maxSaturationFromLastBitmapCreation = this.MaxSaturation;
			m_minValueFromLastBitmapCreation = this.MinValue;
			m_maxValueFromLastBitmapCreation = this.MaxValue;

			Loaded += OnLoaded; // Uno Doc: Added to re-registered disposed event handlers
			Unloaded += OnUnloaded;

			if (SharedHelpers.IsRS1OrHigher())
			{
				IsFocusEngagementEnabled = true;
			}
		}

		// IUIElementOverridesHelper overrides
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ColorSpectrumAutomationPeer(this);
		}

		// IFrameworkElementOverrides overrides
		protected override void OnApplyTemplate()
		{
			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = null;

			m_layoutRoot = GetTemplateChild<Grid>("LayoutRoot");
			m_sizingGrid = GetTemplateChild<Grid>("SizingGrid");
			m_spectrumRectangle = GetTemplateChild<Rectangle>("SpectrumRectangle");
			m_spectrumEllipse = GetTemplateChild<Ellipse>("SpectrumEllipse");
			m_spectrumOverlayRectangle = GetTemplateChild<Rectangle>("SpectrumOverlayRectangle");
			m_spectrumOverlayEllipse = GetTemplateChild<Ellipse>("SpectrumOverlayEllipse");
			m_inputTarget = GetTemplateChild<FrameworkElement>("InputTarget");
			m_selectionEllipsePanel = GetTemplateChild<Panel>("SelectionEllipsePanel");
			m_colorNameToolTip = GetTemplateChild<ToolTip>("ColorNameToolTip");

			// Uno Doc: Extracted event registrations into a separate method, so they can be re-registered on reloading.
			var registrations = SubscribeToEvents();

			if (DownlevelHelper.ToDisplayNameExists())
			{
				if (m_colorNameToolTip is ToolTip colorNameToolTip)
				{
					colorNameToolTip.Content = ColorHelper.ToDisplayName(this.Color);
				}
			}

			if (m_selectionEllipsePanel is Panel selectionEllipsePanel)
			{
				selectionEllipsePanel.RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, OnSelectionEllipseFlowDirectionChanged);
			}

			// If we haven't yet created our bitmaps, do so now.
			if (m_hsvValues.Count == 0)
			{
				CreateBitmapsAndColorMap();
			}
			UpdateEllipse();
			UpdateVisualState(useTransitions: false);

			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = registrations;
			_isTemplateApplied = true;
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

			bool isControlDown = (KeyboardStateTracker.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			ColorPickerHsvChannel incrementChannel = ColorPickerHsvChannel.Hue;

			bool isSaturationValue = false;

			if (args.Key == VirtualKey.Left ||
				args.Key == VirtualKey.Right)
			{
				switch (this.Components)
				{
					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.HueValue:
						incrementChannel = ColorPickerHsvChannel.Hue;
						break;

					case ColorSpectrumComponents.SaturationValue:
						isSaturationValue = true;
						goto case ColorSpectrumComponents.SaturationHue; // fallthrough is explicitly wanted
					case ColorSpectrumComponents.SaturationHue:
						incrementChannel = ColorPickerHsvChannel.Saturation;
						break;

					case ColorSpectrumComponents.ValueHue:
					case ColorSpectrumComponents.ValueSaturation:
						incrementChannel = ColorPickerHsvChannel.Value;
						break;
				}
			}
			else if (args.Key == VirtualKey.Up ||
					 args.Key == VirtualKey.Down)
			{
				switch (this.Components)
				{
					case ColorSpectrumComponents.SaturationHue:
					case ColorSpectrumComponents.ValueHue:
						incrementChannel = ColorPickerHsvChannel.Hue;
						break;

					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.ValueSaturation:
						incrementChannel = ColorPickerHsvChannel.Saturation;
						break;

					case ColorSpectrumComponents.SaturationValue:
						isSaturationValue = true;
						goto case ColorSpectrumComponents.HueValue; // fallthrough is explicitly wanted
					case ColorSpectrumComponents.HueValue:
						incrementChannel = ColorPickerHsvChannel.Value;
						break;
				}
			}

			double minBound = 0.0;
			double maxBound = 0.0;

			switch (incrementChannel)
			{
				case ColorPickerHsvChannel.Hue:
					minBound = this.MinHue;
					maxBound = this.MaxHue;
					break;

				case ColorPickerHsvChannel.Saturation:
					minBound = this.MinSaturation;
					maxBound = this.MaxSaturation;
					break;

				case ColorPickerHsvChannel.Value:
					minBound = this.MinValue;
					maxBound = this.MaxValue;
					break;
			}

			// The order of saturation and value in the spectrum is reversed - the max value is at the bottom while the min value is at the top -
			// so we want left and up to be lower for hue, but higher for saturation and value.
			// This will ensure that the icon always moves in the direction of the key press.
			ColorHelpers.IncrementDirection direction =
				(incrementChannel == ColorPickerHsvChannel.Hue && (args.Key == VirtualKey.Left || args.Key == VirtualKey.Up)) ||
				(incrementChannel != ColorPickerHsvChannel.Hue && (args.Key == VirtualKey.Right || args.Key == VirtualKey.Down)) ?
				ColorHelpers.IncrementDirection.Lower :
				ColorHelpers.IncrementDirection.Higher;

			// Image is flipped in RightToLeft, so we need to invert direction in that case.
			// The combination saturation and value is also flipped, so we need to invert in that case too.
			// If both are false, we don't need to invert.
			// If both are true, we would invert twice, so not invert at all.
			if ((FlowDirection == FlowDirection.RightToLeft) != isSaturationValue &&
				(args.Key == VirtualKey.Left || args.Key == VirtualKey.Right))
			{
				if (direction == ColorHelpers.IncrementDirection.Higher)
				{
					direction = ColorHelpers.IncrementDirection.Lower;
				}
				else
				{
					direction = ColorHelpers.IncrementDirection.Higher;
				}
			}

			ColorHelpers.IncrementAmount amount = isControlDown ? ColorHelpers.IncrementAmount.Large : ColorHelpers.IncrementAmount.Small;

			Vector4 hsvColor = this.HsvColor;
			UpdateColor(ColorHelpers.IncrementColorChannel(new Hsv(Hsv.GetHue(hsvColor), Hsv.GetSaturation(hsvColor), Hsv.GetValue(hsvColor)), incrementChannel, direction, amount, true /* shouldWrap */, minBound, maxBound));
			args.Handled = true;
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			// We only want to bother with the color name tool tip if we can provide color names.
			if (m_colorNameToolTip is ToolTip colorNameToolTip)
			{
				if (DownlevelHelper.ToDisplayNameExists())
				{
					colorNameToolTip.IsOpen = true;
				}
			}

			UpdateVisualState(useTransitions: true);
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			// We only want to bother with the color name tool tip if we can provide color names.
			if (m_colorNameToolTip is ToolTip colorNameToolTip)
			{
				if (DownlevelHelper.ToDisplayNameExists())
				{
					colorNameToolTip.IsOpen = false;
				}
			}

			UpdateVisualState(useTransitions: true);
		}

		// Property changed handler.
		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == ColorProperty)
			{
				OnColorChanged(args);
			}
			else if (property == HsvColorProperty)
			{
				OnHsvColorChanged(args);
			}
			else if (
				property == MinHueProperty ||
				property == MaxHueProperty)
			{
				OnMinMaxHueChanged(args);
			}
			else if (
				property == MinSaturationProperty ||
				property == MaxSaturationProperty)
			{
				OnMinMaxSaturationChanged(args);
			}
			else if (
				property == MinValueProperty ||
				property == MaxValueProperty)
			{
				OnMinMaxValueChanged(args);
			}
			else if (property == ShapeProperty)
			{
				OnShapeChanged(args);
			}
			else if (property == ComponentsProperty)
			{
				OnComponentsChanged(args);
			}
		}

		// DependencyProperty changed event handlers
		private void OnColorChanged(DependencyPropertyChangedEventArgs args)
		{
			// If we're in the process of internally updating the color, then we don't want to respond to the Color property changing.
			if (!m_updatingColor)
			{
				Color color = this.Color;

				m_updatingHsvColor = true;
				Hsv newHsv = ColorConversion.RgbToHsv(new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0));
				this.HsvColor = new Vector4((float)newHsv.H, (float)newHsv.S, (float)newHsv.V, (float)(color.A / 255.0));
				m_updatingHsvColor = false;

				UpdateEllipse();
				UpdateBitmapSources();
			}

			m_oldColor = (Color)args.OldValue;
		}

		private void OnHsvColorChanged(DependencyPropertyChangedEventArgs args)
		{
			// If we're in the process of internally updating the HSV color, then we don't want to respond to the HsvColor property changing.
			if (!m_updatingHsvColor)
			{
				SetColor();
			}

			m_oldHsvColor = (Vector4)args.OldValue;
		}

		private void SetColor()
		{
			Vector4 hsvColor = this.HsvColor;

			m_updatingColor = true;
			Rgb newRgb = ColorConversion.HsvToRgb(new Hsv(Hsv.GetHue(hsvColor), Hsv.GetSaturation(hsvColor), Hsv.GetValue(hsvColor)));

			this.Color = ColorConversion.ColorFromRgba(newRgb, Hsv.GetAlpha(hsvColor));

			m_updatingColor = false;

			UpdateEllipse();
			UpdateBitmapSources();
			RaiseColorChanged();
		}

		public void RaiseColorChanged()
		{
			Color newColor = this.Color;
			var colorChanged =
				m_oldColor.A != newColor.A ||
				m_oldColor.R != newColor.R ||
				m_oldColor.G != newColor.G ||
				m_oldColor.B != newColor.B;
			var areBothColorsBlack =
				(m_oldColor.R == newColor.R && newColor.R == 0) ||
				(m_oldColor.G == newColor.G && newColor.G == 0) ||
				(m_oldColor.B == newColor.B && newColor.B == 0);

			if (colorChanged || areBothColorsBlack)
			{
				var colorChangedEventArgs = new ColorChangedEventArgs();

				colorChangedEventArgs.OldColor = m_oldColor;
				colorChangedEventArgs.NewColor = newColor;

				this.ColorChanged?.Invoke(this, colorChangedEventArgs);

				if (DownlevelHelper.ToDisplayNameExists())
				{
					if (m_colorNameToolTip is ToolTip colorNameToolTip)
					{
						colorNameToolTip.Content = ColorHelper.ToDisplayName(newColor);
					}
				}

				var peer = FrameworkElementAutomationPeer.FromElement(this);
				if (peer != null)
				{
					ColorSpectrumAutomationPeer colorSpectrumPeer = peer as ColorSpectrumAutomationPeer;
					colorSpectrumPeer.RaisePropertyChangedEvent(m_oldColor, newColor, m_oldHsvColor, this.HsvColor);
				}
			}
		}

		protected void OnMinMaxHueChanged(DependencyPropertyChangedEventArgs args)
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

			ColorSpectrumComponents components = this.Components;

			// If hue is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.SaturationValue &&
				components != ColorSpectrumComponents.ValueSaturation)
			{
				CreateBitmapsAndColorMap();
			}
		}

		protected void OnMinMaxSaturationChanged(DependencyPropertyChangedEventArgs args)
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

			ColorSpectrumComponents components = this.Components;

			// If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.HueValue &&
				components != ColorSpectrumComponents.ValueHue)
			{
				CreateBitmapsAndColorMap();
			}
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

			ColorSpectrumComponents components = this.Components;

			// If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.HueSaturation &&
				components != ColorSpectrumComponents.SaturationHue)
			{
				CreateBitmapsAndColorMap();
			}
		}

		private void OnShapeChanged(DependencyPropertyChangedEventArgs args)
		{
			CreateBitmapsAndColorMap();
		}

		private void OnComponentsChanged(DependencyPropertyChangedEventArgs args)
		{
			CreateBitmapsAndColorMap();
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
			// If we're in the middle of creating an image bitmap while being unloaded,
			// we'll want to synchronously cancel it so we don't have any asynchronous actions
			// lingering beyond our lifetime.
			ColorHelpers.CancelAsyncAction(m_createImageBitmapAction);

			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = null;
		}

		public Rect GetBoundingRectangle()
		{
			Rect localRect = new Rect(0, 0, 0, 0);

			if (m_inputTarget is FrameworkElement inputTarget)
			{
				localRect.Width = inputTarget.ActualWidth;
				localRect.Height = inputTarget.ActualHeight;
			}

			var globalBounds = TransformToVisual(null).TransformBounds(localRect);
			return SharedHelpers.ConvertDipsToPhysical(this, globalBounds);
		}

		new private void UpdateVisualState(bool useTransitions)
		{
			if (m_isPointerPressed)
			{
				VisualStateManager.GoToState(this, m_shouldShowLargeSelection ? "PressedLarge" : "Pressed", useTransitions);
			}
			else if (m_isPointerOver)
			{
				VisualStateManager.GoToState(this, "PointerOver", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", useTransitions);
			}

			VisualStateManager.GoToState(this, m_shapeFromLastBitmapCreation == ColorSpectrumShape.Box ? "BoxSelected" : "RingSelected", useTransitions);
			VisualStateManager.GoToState(this, SelectionEllipseShouldBeLight() ? "SelectionEllipseLight" : "SelectionEllipseDark", useTransitions);

			if (this.IsEnabled && this.FocusState != FocusState.Unfocused)
			{
				if (this.FocusState == FocusState.Pointer)
				{
					VisualStateManager.GoToState(this, "PointerFocused", useTransitions);
				}
				else
				{
					VisualStateManager.GoToState(this, "Focused", useTransitions);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, "Unfocused", useTransitions);
			}
		}

		private void UpdateColor(Hsv newHsv)
		{
			m_updatingColor = true;
			m_updatingHsvColor = true;

			Rgb newRgb = ColorConversion.HsvToRgb(newHsv);
			float alpha = Hsv.GetAlpha(this.HsvColor);

			this.Color = ColorConversion.ColorFromRgba(newRgb, alpha);
			this.HsvColor = new Vector4((float)newHsv.H, (float)newHsv.S, (float)newHsv.V, alpha);

			UpdateEllipse();
			UpdateVisualState(useTransitions: true);

			m_updatingHsvColor = false;
			m_updatingColor = false;

			RaiseColorChanged();
		}

		private void UpdateColorFromPoint(PointerPoint point)
		{
			// If we haven't initialized our HSV value array yet, then we should just ignore any user input -
			// we don't yet know what to do with it.
			if (m_hsvValues.Count == 0)
			{
				return;
			}

			double xPosition = point.Position.X;
			double yPosition = point.Position.Y;
			double radius = Math.Min(m_imageWidthFromLastBitmapCreation, m_imageHeightFromLastBitmapCreation) / 2;
			double distanceFromRadius = Math.Sqrt(Math.Pow(xPosition - radius, 2) + Math.Pow(yPosition - radius, 2));

			var shape = this.Shape;

			// If the point is outside the circle, we should bring it back into the circle.
			if (distanceFromRadius > radius && shape == ColorSpectrumShape.Ring)
			{
				xPosition = (radius / distanceFromRadius) * (xPosition - radius) + radius;
				yPosition = (radius / distanceFromRadius) * (yPosition - radius) + radius;
			}

			// Now we need to find the index into the array of HSL values at each point in the spectrum m_image.
			int x = (int)Math.Round(xPosition);
			int y = (int)Math.Round(yPosition);
			int width = (int)Math.Round(m_imageWidthFromLastBitmapCreation);

			if (x < 0)
			{
				x = 0;
			}
			else if (x >= m_imageWidthFromLastBitmapCreation)
			{
				x = (int)Math.Round(m_imageWidthFromLastBitmapCreation) - 1;
			}

			if (y < 0)
			{
				y = 0;
			}
			else if (y >= m_imageHeightFromLastBitmapCreation)
			{
				y = (int)Math.Round(m_imageHeightFromLastBitmapCreation) - 1;
			}

			// The gradient image contains two dimensions of HSL information, but not the third.
			// We should keep the third where it already was.
			// Uno Doc: This can sometimes cause a crash -- possibly due to differences in c# rounding. Therefore, index is now clamped.
			Hsv hsvAtPoint = m_hsvValues[Math.Clamp((y * width + x), 0, m_hsvValues.Count - 1)];

			var components = this.Components;
			var hsvColor = this.HsvColor;

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					hsvAtPoint.S = Hsv.GetSaturation(hsvColor);
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					hsvAtPoint.V = Hsv.GetValue(hsvColor);
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					hsvAtPoint.H = Hsv.GetHue(hsvColor);
					break;
			}

			UpdateColor(hsvAtPoint);
		}

		private void UpdateEllipse()
		{
			var selectionEllipsePanel = m_selectionEllipsePanel;

			if (selectionEllipsePanel == null)
			{
				return;
			}

			// If we don't have an image size yet, we shouldn't be showing the ellipse.
			if (m_imageWidthFromLastBitmapCreation == 0 ||
				m_imageHeightFromLastBitmapCreation == 0)
			{
				selectionEllipsePanel.Visibility = Visibility.Collapsed;
				return;
			}
			else
			{
				selectionEllipsePanel.Visibility = Visibility.Visible;
			}

			double xPosition;
			double yPosition;

			Vector4 hsvColor = this.HsvColor;

			Hsv.SetHue(hsvColor, Math.Clamp(Hsv.GetHue(hsvColor), (float)m_minHueFromLastBitmapCreation, (float)m_maxHueFromLastBitmapCreation));
			Hsv.SetSaturation(hsvColor, Math.Clamp(Hsv.GetSaturation(hsvColor), m_minSaturationFromLastBitmapCreation / 100.0f, m_maxSaturationFromLastBitmapCreation / 100.0f));
			Hsv.SetValue(hsvColor, Math.Clamp(Hsv.GetValue(hsvColor), m_minValueFromLastBitmapCreation / 100.0f, m_maxValueFromLastBitmapCreation / 100.0f));

			if (m_shapeFromLastBitmapCreation == ColorSpectrumShape.Box)
			{
				double xPercent = 0;
				double yPercent = 0;

				double hPercent = (Hsv.GetHue(hsvColor) - m_minHueFromLastBitmapCreation) / (m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation);
				double sPercent = (Hsv.GetSaturation(hsvColor) * 100.0 - m_minSaturationFromLastBitmapCreation) / (m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation);
				double vPercent = (Hsv.GetValue(hsvColor) * 100.0 - m_minValueFromLastBitmapCreation) / (m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation);

				// In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
				// we inverted the direction of that axis in order to put more hue on the outside of the ring,
				// so we need to do similarly here when positioning the ellipse.
				if (m_componentsFromLastBitmapCreation == ColorSpectrumComponents.HueSaturation ||
					m_componentsFromLastBitmapCreation == ColorSpectrumComponents.SaturationHue)
				{
					sPercent = 1 - sPercent;
				}
				else
				{
					vPercent = 1 - vPercent;
				}

				switch (m_componentsFromLastBitmapCreation)
				{
					case ColorSpectrumComponents.HueValue:
						xPercent = hPercent;
						yPercent = vPercent;
						break;

					case ColorSpectrumComponents.HueSaturation:
						xPercent = hPercent;
						yPercent = sPercent;
						break;

					case ColorSpectrumComponents.ValueHue:
						xPercent = vPercent;
						yPercent = hPercent;
						break;

					case ColorSpectrumComponents.ValueSaturation:
						xPercent = vPercent;
						yPercent = sPercent;
						break;

					case ColorSpectrumComponents.SaturationHue:
						xPercent = sPercent;
						yPercent = hPercent;
						break;

					case ColorSpectrumComponents.SaturationValue:
						xPercent = sPercent;
						yPercent = vPercent;
						break;
				}

				xPosition = m_imageWidthFromLastBitmapCreation * xPercent;
				yPosition = m_imageHeightFromLastBitmapCreation * yPercent;
			}
			else
			{
				double thetaValue = 0;
				double rValue = 0;

				double hThetaValue =
					m_maxHueFromLastBitmapCreation != m_minHueFromLastBitmapCreation ?
					360 * (Hsv.GetHue(hsvColor) - m_minHueFromLastBitmapCreation) / (m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation) :
					0;
				double sThetaValue =
					m_maxSaturationFromLastBitmapCreation != m_minSaturationFromLastBitmapCreation ?
					360 * (Hsv.GetSaturation(hsvColor) * 100.0 - m_minSaturationFromLastBitmapCreation) / (m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation) :
					0;
				double vThetaValue =
					m_maxValueFromLastBitmapCreation != m_minValueFromLastBitmapCreation ?
					360 * (Hsv.GetValue(hsvColor) * 100.0 - m_minValueFromLastBitmapCreation) / (m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation) :
					0;
				double hRValue = m_maxHueFromLastBitmapCreation != m_minHueFromLastBitmapCreation ?
					(Hsv.GetHue(hsvColor) - m_minHueFromLastBitmapCreation) / (m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation) - 1 :
					0;
				double sRValue = m_maxSaturationFromLastBitmapCreation != m_minSaturationFromLastBitmapCreation ?
					(Hsv.GetSaturation(hsvColor) * 100.0 - m_minSaturationFromLastBitmapCreation) / (m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation) - 1 :
					0;
				double vRValue = m_maxValueFromLastBitmapCreation != m_minValueFromLastBitmapCreation ?
					(Hsv.GetValue(hsvColor) * 100.0 - m_minValueFromLastBitmapCreation) / (m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation) - 1 :
					0;

				// In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
				// we inverted the direction of that axis in order to put more hue on the outside of the ring,
				// so we need to do similarly here when positioning the ellipse.
				if (m_componentsFromLastBitmapCreation == ColorSpectrumComponents.HueSaturation ||
					m_componentsFromLastBitmapCreation == ColorSpectrumComponents.SaturationHue)
				{
					sThetaValue = 360 - sThetaValue;
					sRValue = -sRValue - 1;
				}
				else
				{
					vThetaValue = 360 - vThetaValue;
					vRValue = -vRValue - 1;
				}

				switch (m_componentsFromLastBitmapCreation)
				{
					case ColorSpectrumComponents.HueValue:
						thetaValue = hThetaValue;
						rValue = vRValue;
						break;

					case ColorSpectrumComponents.HueSaturation:
						thetaValue = hThetaValue;
						rValue = sRValue;
						break;

					case ColorSpectrumComponents.ValueHue:
						thetaValue = vThetaValue;
						rValue = hRValue;
						break;

					case ColorSpectrumComponents.ValueSaturation:
						thetaValue = vThetaValue;
						rValue = sRValue;
						break;

					case ColorSpectrumComponents.SaturationHue:
						thetaValue = sThetaValue;
						rValue = hRValue;
						break;

					case ColorSpectrumComponents.SaturationValue:
						thetaValue = sThetaValue;
						rValue = vRValue;
						break;
				}

				double radius = Math.Min(m_imageWidthFromLastBitmapCreation, m_imageHeightFromLastBitmapCreation) / 2;

				xPosition = (Math.Cos((thetaValue * Math.PI / 180.0) + Math.PI) * radius * rValue) + radius;
				yPosition = (Math.Sin((thetaValue * Math.PI / 180.0) + Math.PI) * radius * rValue) + radius;
			}

			Canvas.SetLeft(selectionEllipsePanel, xPosition - (selectionEllipsePanel.Width / 2));
			Canvas.SetTop(selectionEllipsePanel, yPosition - (selectionEllipsePanel.Height / 2));

			// We only want to bother with the color name tool tip if we can provide color names.
			if (DownlevelHelper.ToDisplayNameExists())
			{
				if (m_colorNameToolTip is ToolTip colorNameToolTip)
				{
					// ToolTip doesn't currently provide any way to re-run its placement logic if its placement target moves,
					// so toggling IsEnabled induces it to do that without incurring any visual glitches.
					colorNameToolTip.IsEnabled = false;
					colorNameToolTip.IsEnabled = true;
				}
			}

			UpdateVisualState(useTransitions: true);
		}

		private void OnLayoutRootSizeChanged(object sender, SizeChangedEventArgs args)
		{
			// We want ColorSpectrum to always be a square, so we'll take the smaller of the dimensions
			// and size the sizing grid to that.
			CreateBitmapsAndColorMap();
		}

		private void OnInputTargetPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			m_isPointerOver = true;
			UpdateVisualState(useTransitions: true);
			args.Handled = true;
		}

		private void OnInputTargetPointerExited(object sender, PointerRoutedEventArgs args)
		{
			m_isPointerOver = false;
			UpdateVisualState(useTransitions: true);
			args.Handled = true;
		}

		private void OnInputTargetPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			var inputTarget = m_inputTarget;

			Focus(FocusState.Pointer);

			m_isPointerPressed = true;
			m_shouldShowLargeSelection =
				(PointerDeviceType)args.Pointer.PointerDeviceType == PointerDeviceType.Pen ||
				(PointerDeviceType)args.Pointer.PointerDeviceType == PointerDeviceType.Touch;

			inputTarget.CapturePointer(args.Pointer, /* uno */ options: PointerCaptureOptions.PreventDirectManipulation);
			UpdateColorFromPoint(args.GetCurrentPoint(inputTarget));
			UpdateVisualState(useTransitions: true);
			UpdateEllipse();

			args.Handled = true;
		}

		private void OnInputTargetPointerMoved(object sender, PointerRoutedEventArgs args)
		{
			if (!m_isPointerPressed)
			{
				return;
			}

			UpdateColorFromPoint(args.GetCurrentPoint(m_inputTarget));
			args.Handled = true;
		}

		private void OnInputTargetPointerReleased(object sender, PointerRoutedEventArgs args)
		{
			m_isPointerPressed = false;
			m_shouldShowLargeSelection = false;

			m_inputTarget.ReleasePointerCapture(args.Pointer);
			UpdateVisualState(useTransitions: true);
			UpdateEllipse();

			args.Handled = true;
		}

		private void OnSelectionEllipseFlowDirectionChanged(DependencyObject o, DependencyProperty p)
		{
			UpdateEllipse();
		}

		private CompositeDisposable SubscribeToEvents()
		{
			var registrations = new CompositeDisposable();

			if (m_layoutRoot is Grid layoutRoot)
			{
				layoutRoot.SizeChanged += OnLayoutRootSizeChanged;
				registrations.Add(() => layoutRoot.SizeChanged -= OnLayoutRootSizeChanged);
			}

			if (m_inputTarget is FrameworkElement inputTarget)
			{
				inputTarget.PointerEntered += OnInputTargetPointerEntered;
				inputTarget.PointerExited += OnInputTargetPointerExited;
				inputTarget.PointerPressed += OnInputTargetPointerPressed;
				inputTarget.PointerMoved += OnInputTargetPointerMoved;
				inputTarget.PointerReleased += OnInputTargetPointerReleased;

				registrations.Add(() => inputTarget.PointerEntered -= OnInputTargetPointerEntered);
				registrations.Add(() => inputTarget.PointerExited -= OnInputTargetPointerExited);
				registrations.Add(() => inputTarget.PointerPressed -= OnInputTargetPointerPressed);
				registrations.Add(() => inputTarget.PointerMoved -= OnInputTargetPointerMoved);
				registrations.Add(() => inputTarget.PointerReleased -= OnInputTargetPointerReleased);
			}

			return registrations;
		}

		private async void CreateBitmapsAndColorMap()
		{
			var layoutRoot = m_layoutRoot;
			var sizingGrid = m_sizingGrid;
			var inputTarget = m_inputTarget;
			var spectrumRectangle = m_spectrumRectangle;
			var spectrumEllipse = m_spectrumEllipse;
			var spectrumOverlayRectangle = m_spectrumOverlayRectangle;
			var spectrumOverlayEllipse = m_spectrumOverlayEllipse;

			if (m_layoutRoot == null ||
				m_sizingGrid == null ||
				m_inputTarget == null ||
				m_spectrumRectangle == null ||
				m_spectrumEllipse == null ||
				m_spectrumOverlayRectangle == null ||
				m_spectrumOverlayEllipse == null ||
				SharedHelpers.IsInDesignMode())
			{
				return;
			}

			double minDimension = Math.Min(layoutRoot.ActualWidth, layoutRoot.ActualHeight);

			if (minDimension == 0)
			{
				return;
			}

			sizingGrid.Width = minDimension;
			sizingGrid.Height = minDimension;

			// Uno Doc: Slightly modified from WinUI
			if (sizingGrid.Clip is RectangleGeometry clip)
			{
				clip.Rect = new Rect(0, 0, minDimension, minDimension);
			}

			inputTarget.Width = minDimension;
			inputTarget.Height = minDimension;
			spectrumRectangle.Width = minDimension;
			spectrumRectangle.Height = minDimension;
			spectrumEllipse.Width = minDimension;
			spectrumEllipse.Height = minDimension;
			spectrumOverlayRectangle.Width = minDimension;
			spectrumOverlayRectangle.Height = minDimension;
			spectrumOverlayEllipse.Width = minDimension;
			spectrumOverlayEllipse.Height = minDimension;

			Vector4 hsvColor = this.HsvColor;
			int minHue = this.MinHue;
			int maxHue = this.MaxHue;
			int minSaturation = this.MinSaturation;
			int maxSaturation = this.MaxSaturation;
			int minValue = this.MinValue;
			int maxValue = this.MaxValue;
			ColorSpectrumShape shape = this.Shape;
			ColorSpectrumComponents components = this.Components;

			// If min >= max, then by convention, min is the only number that a property can have.
			if (minHue >= maxHue)
			{
				maxHue = minHue;
			}

			if (minSaturation >= maxSaturation)
			{
				maxSaturation = minSaturation;
			}

			if (minValue >= maxValue)
			{
				maxValue = minValue;
			}

			Hsv hsv = new Hsv(Hsv.GetHue(hsvColor), Hsv.GetSaturation(hsvColor), Hsv.GetValue(hsvColor));

			// The middle 4 are only needed and used in the case of hue as the third dimension.
			// Saturation and luminosity need only a min and max.
			ArrayList<byte> bgraMinPixelData = new ArrayList<byte>();
			ArrayList<byte> bgraMiddle1PixelData = new ArrayList<byte>();
			ArrayList<byte> bgraMiddle2PixelData = new ArrayList<byte>();
			ArrayList<byte> bgraMiddle3PixelData = new ArrayList<byte>();
			ArrayList<byte> bgraMiddle4PixelData = new ArrayList<byte>();
			ArrayList<byte> bgraMaxPixelData = new ArrayList<byte>();
			List<Hsv> newHsvValues = new List<Hsv>();

			// Uno Docs: size_t not available in C# so types were changed
			var pixelCount = (int)(Math.Round(minDimension) * Math.Round(minDimension));
			var pixelDataSize = pixelCount * 4;
			bgraMinPixelData.Capacity = pixelDataSize;

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
				components == ColorSpectrumComponents.SaturationValue)
			{
				bgraMiddle1PixelData.Capacity = pixelDataSize;
				bgraMiddle2PixelData.Capacity = pixelDataSize;
				bgraMiddle3PixelData.Capacity = pixelDataSize;
				bgraMiddle4PixelData.Capacity = pixelDataSize;
			}

			bgraMaxPixelData.Capacity = pixelDataSize;
			newHsvValues.Capacity = pixelCount;

			int minDimensionInt = (int)Math.Round(minDimension);
			//WorkItemHandler workItemHandler =
			//(IAsyncAction workItem) =>
			await Task.Run(() =>
			{
				// As the user perceives it, every time the third dimension not represented in the ColorSpectrum changes,
				// the ColorSpectrum will visually change to accommodate that value.  For example, if the ColorSpectrum handles hue and luminosity,
				// and the saturation externally goes from 1.0 to 0.5, then the ColorSpectrum will visually change to look more washed out
				// to represent that third dimension's new value.
				// Internally, however, we don't want to regenerate the ColorSpectrum bitmap every single time this happens, since that's very expensive.
				// In order to make it so that we don't have to, we implement an optimization where, rather than having only one bitmap,
				// we instead have multiple that we blend together using opacity to create the effect that we want.
				// In the case where the third dimension is saturation or luminosity, we only need two: one bitmap at the minimum value
				// of the third dimension, and one bitmap at the maximum.  Then we set the second's opacity at whatever the value of
				// the third dimension is - e.g., a saturation of 0.5 implies an opacity of 50%.
				// In the case where the third dimension is hue, we need six: one bitmap corresponding to red, yellow, green, cyan, blue, and purple.
				// We'll then blend between whichever colors our hue exists between - e.g., an orange color would use red and yellow with an opacity of 50%.
				// This optimization does incur slightly more startup time initially since we have to generate multiple bitmaps at once instead of only one,
				// but the running time savings after that are *huge* when we can just set an opacity instead of generating a brand new bitmap.
				if (shape == ColorSpectrumShape.Box)
				{
					for (int x = minDimensionInt - 1; x >= 0; --x)
					{
						for (int y = minDimensionInt - 1; y >= 0; --y)
						{
							//if (workItem.Status == AsyncStatus.Canceled)
							//{
							//	break;
							//}

							FillPixelForBox(
								x, y, hsv, minDimensionInt, components, minHue, maxHue, minSaturation, maxSaturation, minValue, maxValue,
								bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData, bgraMiddle4PixelData, bgraMaxPixelData,
								newHsvValues);
						}
					}
				}
				else
				{
					for (int y = 0; y < minDimensionInt; ++y)
					{
						for (int x = 0; x < minDimensionInt; ++x)
						{
							//if (workItem.Status == AsyncStatus.Canceled)
							//{
							//	break;
							//}

							FillPixelForRing(
								x, y, minDimensionInt / 2.0, hsv, components, minHue, maxHue, minSaturation, maxSaturation, minValue, maxValue,
								bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData, bgraMiddle4PixelData, bgraMaxPixelData,
								newHsvValues);
						}
					}
				}
			});

			//if (m_createImageBitmapAction != null)
			//{
			//	m_createImageBitmapAction.Cancel();
			//}

			//m_createImageBitmapAction = ThreadPool.RunAsync(workItemHandler);
			var strongThis = this;
			//m_createImageBitmapAction.Completed = new AsyncActionCompletedHandler(
			//(IAsyncAction asyncInfo, AsyncStatus asyncStatus) =>
			//{
			//	if (asyncStatus != AsyncStatus.Completed)
			//	{
			//		return;
			//	}

			strongThis.m_createImageBitmapAction = null;

			// Uno Doc: Assumed normal priority is acceptable
			_ = strongThis.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				int pixelWidth = (int)Math.Round(minDimension);
				int pixelHeight = (int)Math.Round(minDimension);

				// Uno Doc: In C# (unlike C++) the existing 'components' var is captured by the lambda so it must be renamed here
				ColorSpectrumComponents components2 = strongThis.Components;

				// UNO TODO
				//if (SharedHelpers.IsRS2OrHigher)
				//{
				//	LoadedImageSurface minSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMinPixelData);
				//	LoadedImageSurface maxSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMaxPixelData);

				//	switch (components2)
				//	{
				//		case ColorSpectrumComponents.HueValue:
				//		case ColorSpectrumComponents.ValueHue:
				//			strongThis.m_saturationMinimumSurface = minSurface;
				//			strongThis.m_saturationMaximumSurface = maxSurface;
				//			break;
				//		case ColorSpectrumComponents.HueSaturation:
				//		case ColorSpectrumComponents.SaturationHue:
				//			strongThis.m_valueSurface = maxSurface;
				//			break;
				//		case ColorSpectrumComponents.ValueSaturation:
				//		case ColorSpectrumComponents.SaturationValue:
				//			strongThis.m_hueRedSurface = minSurface;
				//			strongThis.m_hueYellowSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMiddle1PixelData);
				//			strongThis.m_hueGreenSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMiddle2PixelData);
				//			strongThis.m_hueCyanSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMiddle3PixelData);
				//			strongThis.m_hueBlueSurface = CreateSurfaceFromPixelData(pixelWidth, pixelHeight, bgraMiddle4PixelData);
				//			strongThis.m_huePurpleSurface = maxSurface;
				//			break;
				//	}
				//}
				//else
				{
					WriteableBitmap minBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMinPixelData);
					WriteableBitmap maxBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMaxPixelData);

					switch (components2)
					{
						case ColorSpectrumComponents.HueValue:
						case ColorSpectrumComponents.ValueHue:
							strongThis.m_saturationMinimumBitmap = minBitmap;
							strongThis.m_saturationMaximumBitmap = maxBitmap;
							break;
						case ColorSpectrumComponents.HueSaturation:
						case ColorSpectrumComponents.SaturationHue:
							strongThis.m_valueBitmap = maxBitmap;
							break;
						case ColorSpectrumComponents.ValueSaturation:
						case ColorSpectrumComponents.SaturationValue:
							strongThis.m_hueRedBitmap = minBitmap;
							strongThis.m_hueYellowBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle1PixelData);
							strongThis.m_hueGreenBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle2PixelData);
							strongThis.m_hueCyanBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle3PixelData);
							strongThis.m_hueBlueBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle4PixelData);
							strongThis.m_huePurpleBitmap = maxBitmap;
							break;
					}
				}

				strongThis.m_shapeFromLastBitmapCreation = strongThis.Shape;
				strongThis.m_componentsFromLastBitmapCreation = strongThis.Components;
				strongThis.m_imageWidthFromLastBitmapCreation = minDimension;
				strongThis.m_imageHeightFromLastBitmapCreation = minDimension;
				strongThis.m_minHueFromLastBitmapCreation = strongThis.MinHue;
				strongThis.m_maxHueFromLastBitmapCreation = strongThis.MaxHue;
				strongThis.m_minSaturationFromLastBitmapCreation = strongThis.MinSaturation;
				strongThis.m_maxSaturationFromLastBitmapCreation = strongThis.MaxSaturation;
				strongThis.m_minValueFromLastBitmapCreation = strongThis.MinValue;
				strongThis.m_maxValueFromLastBitmapCreation = strongThis.MaxValue;

				strongThis.m_hsvValues = newHsvValues;

				strongThis.UpdateBitmapSources();
				strongThis.UpdateEllipse();
			});
			//});
		}

		private void FillPixelForBox(
			double x,
			double y,
			Hsv baseHsv,
			double minDimension,
			ColorSpectrumComponents components,
			double minHue,
			double maxHue,
			double minSaturation,
			double maxSaturation,
			double minValue,
			double maxValue,
			ArrayList<byte> bgraMinPixelData,
			ArrayList<byte> bgraMiddle1PixelData,
			ArrayList<byte> bgraMiddle2PixelData,
			ArrayList<byte> bgraMiddle3PixelData,
			ArrayList<byte> bgraMiddle4PixelData,
			ArrayList<byte> bgraMaxPixelData,
			List<Hsv> newHsvValues)
		{
			double hMin = minHue;
			double hMax = maxHue;
			double sMin = minSaturation / 100.0;
			double sMax = maxSaturation / 100.0;
			double vMin = minValue / 100.0;
			double vMax = maxValue / 100.0;

			Hsv hsvMin = baseHsv;
			Hsv hsvMiddle1 = baseHsv;
			Hsv hsvMiddle2 = baseHsv;
			Hsv hsvMiddle3 = baseHsv;
			Hsv hsvMiddle4 = baseHsv;
			Hsv hsvMax = baseHsv;

			double xPercent = (minDimension - 1 - x) / (minDimension - 1);
			double yPercent = (minDimension - 1 - y) / (minDimension - 1);

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.HueSaturation:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.ValueHue:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.ValueSaturation:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;

				case ColorSpectrumComponents.SaturationHue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.SaturationValue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;
			}

			// If saturation is an axis in the spectrum with hue, or value is an axis, then we want
			// that axis to go from maximum at the top to minimum at the bottom,
			// or maximum at the outside to minimum at the inside in the case of the ring configuration,
			// so we'll invert the number before assigning the HSL value to the array.
			// Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
			// in the case of the ring configuration.
			if (components == ColorSpectrumComponents.HueSaturation ||
				components == ColorSpectrumComponents.SaturationHue)
			{
				hsvMin.S = sMax - hsvMin.S + sMin;
				hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
				hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
				hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
				hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
				hsvMax.S = sMax - hsvMax.S + sMin;
			}
			else
			{
				hsvMin.V = vMax - hsvMin.V + vMin;
				hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
				hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
				hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
				hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
				hsvMax.V = vMax - hsvMax.V + vMin;
			}

			newHsvValues.Add(hsvMin);

			Rgb rgbMin = ColorConversion.HsvToRgb(hsvMin);
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.B * 255.0)); // b
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.G * 255.0)); // g
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.R * 255.0)); // r
			bgraMinPixelData.Add(255); // a - ignored

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
				components == ColorSpectrumComponents.SaturationValue)
			{
				Rgb rgbMiddle1 = ColorConversion.HsvToRgb(hsvMiddle1);
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.B * 255.0)); // b
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.G * 255.0)); // g
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.R * 255.0)); // r
				bgraMiddle1PixelData.Add(255); // a - ignored

				Rgb rgbMiddle2 = ColorConversion.HsvToRgb(hsvMiddle2);
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.B * 255.0)); // b
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.G * 255.0)); // g
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.R * 255.0)); // r
				bgraMiddle2PixelData.Add(255); // a - ignored

				Rgb rgbMiddle3 = ColorConversion.HsvToRgb(hsvMiddle3);
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.B * 255.0)); // b
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.G * 255.0)); // g
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.R * 255.0)); // r
				bgraMiddle3PixelData.Add(255); // a - ignored

				Rgb rgbMiddle4 = ColorConversion.HsvToRgb(hsvMiddle4);
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.B * 255.0)); // b
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.G * 255.0)); // g
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.R * 255.0)); // r
				bgraMiddle4PixelData.Add(255); // a - ignored
			}

			Rgb rgbMax = ColorConversion.HsvToRgb(hsvMax);
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.B * 255.0)); // b
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.G * 255.0)); // g
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.R * 255.0)); // r
			bgraMaxPixelData.Add(255); // a - ignored
		}

		private void FillPixelForRing(
			double x,
			double y,
			double radius,
			Hsv baseHsv,
			ColorSpectrumComponents components,
			double minHue,
			double maxHue,
			double minSaturation,
			double maxSaturation,
			double minValue,
			double maxValue,
			ArrayList<byte> bgraMinPixelData,
			ArrayList<byte> bgraMiddle1PixelData,
			ArrayList<byte> bgraMiddle2PixelData,
			ArrayList<byte> bgraMiddle3PixelData,
			ArrayList<byte> bgraMiddle4PixelData,
			ArrayList<byte> bgraMaxPixelData,
			List<Hsv> newHsvValues)
		{
			double hMin = minHue;
			double hMax = maxHue;
			double sMin = minSaturation / 100.0;
			double sMax = maxSaturation / 100.0;
			double vMin = minValue / 100.0;
			double vMax = maxValue / 100.0;

			double distanceFromRadius = Math.Sqrt(Math.Pow(x - radius, 2) + Math.Pow(y - radius, 2));

			double xToUse = x;
			double yToUse = y;

			// If we're outside the ring, then we want the pixel to appear as blank.
			// However, to avoid issues with rounding errors, we'll act as though this point
			// is on the edge of the ring for the purposes of returning an HSL value.
			// That way, hittesting on the edges will always return the correct value.
			if (distanceFromRadius > radius)
			{
				xToUse = (radius / distanceFromRadius) * (x - radius) + radius;
				yToUse = (radius / distanceFromRadius) * (y - radius) + radius;
				distanceFromRadius = radius;
			}

			Hsv hsvMin = baseHsv;
			Hsv hsvMiddle1 = baseHsv;
			Hsv hsvMiddle2 = baseHsv;
			Hsv hsvMiddle3 = baseHsv;
			Hsv hsvMiddle4 = baseHsv;
			Hsv hsvMax = baseHsv;

			double r = 1 - distanceFromRadius / radius;

			double theta = Math.Atan2((radius - yToUse), (radius - xToUse)) * 180.0 / Math.PI;
			theta += 180.0;
			theta = Math.Floor(theta);

			while (theta > 360)
			{
				theta -= 360;
			}

			double thetaPercent = theta / 360;

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.HueSaturation:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.ValueHue:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.ValueSaturation:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;

				case ColorSpectrumComponents.SaturationHue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.SaturationValue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;
			}

			// If saturation is an axis in the spectrum with hue, or value is an axis, then we want
			// that axis to go from maximum at the top to minimum at the bottom,
			// or maximum at the outside to minimum at the inside in the case of the ring configuration,
			// so we'll invert the number before assigning the HSL value to the array.
			// Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
			// in the case of the ring configuration.
			if (components == ColorSpectrumComponents.HueSaturation ||
				components == ColorSpectrumComponents.SaturationHue)
			{
				hsvMin.S = sMax - hsvMin.S + sMin;
				hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
				hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
				hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
				hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
				hsvMax.S = sMax - hsvMax.S + sMin;
			}
			else
			{
				hsvMin.V = vMax - hsvMin.V + vMin;
				hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
				hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
				hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
				hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
				hsvMax.V = vMax - hsvMax.V + vMin;
			}

			newHsvValues.Add(hsvMin);

			Rgb rgbMin = ColorConversion.HsvToRgb(hsvMin);
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.B * 255)); // b
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.G * 255)); // g
			bgraMinPixelData.Add((byte)Math.Round(rgbMin.R * 255)); // r
			bgraMinPixelData.Add(255); // a

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
				components == ColorSpectrumComponents.SaturationValue)
			{
				Rgb rgbMiddle1 = ColorConversion.HsvToRgb(hsvMiddle1);
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.B * 255)); // b
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.G * 255)); // g
				bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.R * 255)); // r
				bgraMiddle1PixelData.Add(255); // a

				Rgb rgbMiddle2 = ColorConversion.HsvToRgb(hsvMiddle2);
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.B * 255)); // b
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.G * 255)); // g
				bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.R * 255)); // r
				bgraMiddle2PixelData.Add(255); // a

				Rgb rgbMiddle3 = ColorConversion.HsvToRgb(hsvMiddle3);
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.B * 255)); // b
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.G * 255)); // g
				bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.R * 255)); // r
				bgraMiddle3PixelData.Add(255); // a

				Rgb rgbMiddle4 = ColorConversion.HsvToRgb(hsvMiddle4);
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.B * 255)); // b
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.G * 255)); // g
				bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.R * 255)); // r
				bgraMiddle4PixelData.Add(255); // a
			}

			Rgb rgbMax = ColorConversion.HsvToRgb(hsvMax);
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.B * 255)); // b
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.G * 255)); // g
			bgraMaxPixelData.Add((byte)Math.Round(rgbMax.R * 255)); // r
			bgraMaxPixelData.Add(255); // a
		}

		private void UpdateBitmapSources()
		{
			var spectrumOverlayRectangle = m_spectrumOverlayRectangle;
			var spectrumOverlayEllipse = m_spectrumOverlayEllipse;

			if (spectrumOverlayRectangle == null ||
				spectrumOverlayEllipse == null)
			{
				return;
			}

			var spectrumRectangle = m_spectrumRectangle;
			var spectrumEllipse = m_spectrumEllipse;

			Vector4 hsvColor = this.HsvColor;
			ColorSpectrumComponents components = this.Components;

			// We'll set the base image and the overlay image based on which component is our third dimension.
			// If it's saturation or luminosity, then the base image is that dimension at its minimum value,
			// while the overlay image is that dimension at its maximum value.
			// If it's hue, then we'll figure out where in the color wheel we are, and then use the two
			// colors on either side of our position as our base image and overlay image.
			// For example, if our hue is orange, then the base image would be red and the overlay image yellow.
			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					// UNO TODO
					//if (SharedHelpers.IsRS2OrHigher)
					//{
					//	if (m_saturationMinimumSurface == null ||
					//		m_saturationMaximumSurface == null)
					//	{
					//		return;
					//	}

					//	winrt::SpectrumBrush spectrumBrush{ winrt::make<SpectrumBrush>() };

					//	spectrumBrush.MinSurface(m_saturationMinimumSurface);
					//	spectrumBrush.MaxSurface(m_saturationMaximumSurface);
					//	spectrumBrush.MaxSurfaceOpacity(Hsv.GetSaturation(hsvColor));
					//	spectrumRectangle.Fill = spectrumBrush;
					//	spectrumEllipse.Fill = spectrumBrush;
					//}
					//else
					{
						if (m_saturationMinimumBitmap == null ||
							m_saturationMaximumBitmap == null)
						{
							return;
						}

						ImageBrush spectrumBrush = new ImageBrush();
						ImageBrush spectrumOverlayBrush = new ImageBrush();

						spectrumBrush.ImageSource = m_saturationMinimumBitmap;
						spectrumOverlayBrush.ImageSource = m_saturationMaximumBitmap;
						spectrumOverlayRectangle.Opacity = Hsv.GetSaturation(hsvColor);
						spectrumOverlayEllipse.Opacity = Hsv.GetSaturation(hsvColor);
						spectrumRectangle.Fill = spectrumBrush;
						spectrumEllipse.Fill = spectrumBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					}
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					// UNO TODO
					//if (SharedHelpers.IsRS2OrHigher)
					//{
					//	if (m_valueSurface == null)
					//	{
					//		return;
					//	}

					//	SpectrumBrush spectrumBrush{ winrt::make<SpectrumBrush>() };

					//	spectrumBrush.MinSurface(m_valueSurface);
					//	spectrumBrush.MaxSurface(m_valueSurface);
					//	spectrumBrush.MaxSurfaceOpacity(1);
					//	spectrumRectangle.Fill = spectrumBrush;
					//	spectrumEllipse.Fill = spectrumBrush;
					//}
					//else
					{
						if (m_valueBitmap == null)
						{
							return;
						}

						ImageBrush spectrumBrush = new ImageBrush();
						ImageBrush spectrumOverlayBrush = new ImageBrush();

						spectrumBrush.ImageSource = m_valueBitmap;
						spectrumOverlayBrush.ImageSource = m_valueBitmap;
						spectrumOverlayRectangle.Opacity = 1.0;
						spectrumOverlayEllipse.Opacity = 1.0;
						spectrumRectangle.Fill = spectrumBrush;
						spectrumEllipse.Fill = spectrumBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					}
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					// UNO TODO
					//if (SharedHelpers.IsRS2OrHigher)
					//{
					//	if (m_hueRedSurface == null ||
					//		m_hueYellowSurface == null ||
					//		m_hueGreenSurface == null ||
					//		m_hueCyanSurface == null ||
					//		m_hueBlueSurface == null ||
					//		m_huePurpleSurface == null)
					//	{
					//		return;
					//	}

					//	SpectrumBrush spectrumBrush{ winrt::make<SpectrumBrush>() };

					//	double sextant = Hsv.GetHue(hsvColor) / 60.0;

					//	if (sextant < 1)
					//	{
					//		spectrumBrush.MinSurface(m_hueRedSurface);
					//		spectrumBrush.MaxSurface(m_hueYellowSurface);
					//	}
					//	else if (sextant >= 1 && sextant < 2)
					//	{
					//		spectrumBrush.MinSurface(m_hueYellowSurface);
					//		spectrumBrush.MaxSurface(m_hueGreenSurface);
					//	}
					//	else if (sextant >= 2 && sextant < 3)
					//	{
					//		spectrumBrush.MinSurface(m_hueGreenSurface);
					//		spectrumBrush.MaxSurface(m_hueCyanSurface);
					//	}
					//	else if (sextant >= 3 && sextant < 4)
					//	{
					//		spectrumBrush.MinSurface(m_hueCyanSurface);
					//		spectrumBrush.MaxSurface(m_hueBlueSurface);
					//	}
					//	else if (sextant >= 4 && sextant < 5)
					//	{
					//		spectrumBrush.MinSurface(m_hueBlueSurface);
					//		spectrumBrush.MaxSurface(m_huePurpleSurface);
					//	}
					//	else
					//	{
					//		spectrumBrush.MinSurface(m_huePurpleSurface);
					//		spectrumBrush.MaxSurface(m_hueRedSurface);
					//	}

					//	spectrumBrush.MaxSurfaceOpacity(sextant - (int)sextant);
					//	spectrumRectangle.Fill = spectrumBrush;
					//	spectrumEllipse.Fill = spectrumBrush;
					//}
					//else
					{
						if (m_hueRedBitmap == null ||
							m_hueYellowBitmap == null ||
							m_hueGreenBitmap == null ||
							m_hueCyanBitmap == null ||
							m_hueBlueBitmap == null ||
							m_huePurpleBitmap == null)
						{
							return;
						}

						ImageBrush spectrumBrush = new ImageBrush();
						ImageBrush spectrumOverlayBrush = new ImageBrush();

						double sextant = Hsv.GetHue(hsvColor) / 60.0;

						if (sextant < 1)
						{
							spectrumBrush.ImageSource = m_hueRedBitmap;
							spectrumOverlayBrush.ImageSource = m_hueYellowBitmap;
						}
						else if (sextant >= 1 && sextant < 2)
						{
							spectrumBrush.ImageSource = m_hueYellowBitmap;
							spectrumOverlayBrush.ImageSource = m_hueGreenBitmap;
						}
						else if (sextant >= 2 && sextant < 3)
						{
							spectrumBrush.ImageSource = m_hueGreenBitmap;
							spectrumOverlayBrush.ImageSource = m_hueCyanBitmap;
						}
						else if (sextant >= 3 && sextant < 4)
						{
							spectrumBrush.ImageSource = m_hueCyanBitmap;
							spectrumOverlayBrush.ImageSource = m_hueBlueBitmap;
						}
						else if (sextant >= 4 && sextant < 5)
						{
							spectrumBrush.ImageSource = m_hueBlueBitmap;
							spectrumOverlayBrush.ImageSource = m_huePurpleBitmap;
						}
						else
						{
							spectrumBrush.ImageSource = m_huePurpleBitmap;
							spectrumOverlayBrush.ImageSource = m_hueRedBitmap;
						}

						spectrumOverlayRectangle.Opacity = sextant - (int)sextant;
						spectrumOverlayEllipse.Opacity = sextant - (int)sextant;
						spectrumRectangle.Fill = spectrumBrush;
						spectrumEllipse.Fill = spectrumBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
						spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					}
					break;
			}
		}

		private bool SelectionEllipseShouldBeLight()
		{
			// The selection ellipse should be light if and only if the chosen color
			// contrasts more with black than it does with white.
			// To find how much something contrasts with white, we use the equation
			// for relative luminance, which is given by
			//
			// L = 0.2126 * Rg + 0.7152 * Gg + 0.0722 * Bg
			//
			// where Xg = { X/3294 if X <= 10, (R/269 + 0.0513)^2.4 otherwise }
			//
			// If L is closer to 1, then the color is closer to white; if it is closer to 0,
			// then the color is closer to black.  This is based on the fact that the human
			// eye perceives green to be much brighter than red, which in turn is perceived to be
			// brighter than blue.
			//
			// If the third dimension is value, then we won't be updating the spectrum's displayed colors,
			// so in that case we should use a value of 1 when considering the backdrop
			// for the selection ellipse.
			Color displayedColor;

			if (this.Components == ColorSpectrumComponents.HueSaturation ||
				this.Components == ColorSpectrumComponents.SaturationHue)
			{
				Vector4 hsvColor = this.HsvColor;
				Rgb color = ColorConversion.HsvToRgb(new Hsv(Hsv.GetHue(hsvColor), Hsv.GetSaturation(hsvColor), 1.0));
				displayedColor = ColorConversion.ColorFromRgba(color, Hsv.GetAlpha(hsvColor));
			}
			else
			{
				displayedColor = this.Color;
			}

			double rg = displayedColor.R <= 10 ? displayedColor.R / 3294.0 : Math.Pow(displayedColor.R / 269.0 + 0.0513, 2.4);
			double gg = displayedColor.G <= 10 ? displayedColor.G / 3294.0 : Math.Pow(displayedColor.G / 269.0 + 0.0513, 2.4);
			double bg = displayedColor.B <= 10 ? displayedColor.B / 3294.0 : Math.Pow(displayedColor.B / 269.0 + 0.0513, 2.4);

			return 0.2126 * rg + 0.7152 * gg + 0.0722 * bg <= 0.5;
		}
	}
}
