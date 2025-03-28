using System;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

using ColorChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorChangedEventArgs;
using ColorPicker = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorPicker;
using ColorSpectrum = Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.ColorSpectrum;
using ColorSpectrumComponents = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorSpectrumComponents;
using ColorSpectrumShape = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorSpectrumShape;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.ColorPickerTests
{
	[Sample("ColorPicker", "MUX", Name = "WinUIColorPickerPage")]
	public sealed partial class WinUIColorPickerPage : UserControl
	{
		ToolTip colorNameToolTip;
		Rectangle spectrumRectangle;
		FrameworkElement colorSpectrumInputTarget;
		Rectangle previousColorRectangle;
		Panel selectionEllipsePanel;
		Ellipse selectionEllipse;
		ButtonBase moreButton;

		public WinUIColorPickerPage()
		{
			this.InitializeComponent();

			// Initialize the ColorPicker to a known default color so we have a known starting point.
			this.ColorPicker.Color = Colors.Red;

			this.ColorPicker.Loaded += ColorPicker_Loaded;

			spectrumRectangle = null;
			colorSpectrumInputTarget = null;
			previousColorRectangle = null;
			moreButton = null;

			this.RedTextBlock.Text = this.ColorPicker.Color.R.ToString();
			this.GreenTextBlock.Text = this.ColorPicker.Color.G.ToString();
			this.BlueTextBlock.Text = this.ColorPicker.Color.B.ToString();
			this.AlphaTextBlock.Text = this.ColorPicker.Color.A.ToString();

			var verifySubclass = new MyColorSpectrum();

		}

		private void ColorPicker_Loaded(object sender, RoutedEventArgs e)
		{
			FindAndGiveAutomationIdToVisualChild("ColorSpectrum");

			spectrumRectangle = FindVisualChildByName(this.ColorPicker, "SpectrumRectangle") as Rectangle;

			if (spectrumRectangle != null)
			{
				spectrumRectangle.RegisterPropertyChangedCallback(Shape.FillProperty, new DependencyPropertyChangedCallback(SpectrumRectangleFillChanged));
			}

			colorSpectrumInputTarget = FindVisualChildByName(this.ColorPicker, "InputTarget") as FrameworkElement;

			if (colorSpectrumInputTarget != null)
			{
				UpdateHeightFromInputTarget();
				colorSpectrumInputTarget.SizeChanged += ColorSpectrumImage_SizeChanged;
			}

			ComboBox comboBox = FindVisualChildByName(this.ColorPicker, "ColorRepresentationComboBox") as ComboBox;

			if (comboBox != null)
			{
				AutomationProperties.SetAutomationId(comboBox.Items[0] as DependencyObject, "RGBComboBoxItem");
				AutomationProperties.SetAutomationId(comboBox.Items[1] as DependencyObject, "HSVComboBoxItem");
			}

			previousColorRectangle = FindVisualChildByName(this.ColorPicker, "PreviousColorRectangle") as Rectangle;

			if (previousColorRectangle != null)
			{
				previousColorRectangle.RegisterPropertyChangedCallback(Rectangle.FillProperty, new DependencyPropertyChangedCallback(PreviousColorRectangleFillChanged));
			}

			selectionEllipsePanel = FindVisualChildByName(this.ColorPicker, "SelectionEllipsePanel") as Panel;

			if (selectionEllipsePanel != null)
			{
				selectionEllipsePanel.RegisterPropertyChangedCallback(Canvas.LeftProperty, new DependencyPropertyChangedCallback(SelectionEllipsePositionChanged));
				selectionEllipsePanel.RegisterPropertyChangedCallback(Canvas.TopProperty, new DependencyPropertyChangedCallback(SelectionEllipsePositionChanged));

				UpdateSelectionEllipsePosition();
			}

			selectionEllipse = FindVisualChildByName(this.ColorPicker, "SelectionEllipse") as Ellipse;

			if (selectionEllipse != null)
			{
				selectionEllipse.RegisterPropertyChangedCallback(Ellipse.StrokeProperty, new DependencyPropertyChangedCallback(SelectionEllipseStrokeChanged));

				UpdateSelectionEllipseColor();

				colorNameToolTip = ToolTipService.GetToolTip(selectionEllipse) as ToolTip;

				if (colorNameToolTip != null)
				{
					// Uno Doc: ToolTip must be fully qualified for iOS/macOS where NSView.ToolTip also exists
					colorNameToolTip.RegisterPropertyChangedCallback(Windows.UI.Xaml.Controls.ToolTip.ContentProperty, new DependencyPropertyChangedCallback(ColorNameToolTipContentChanged));
					UpdateSelectedColorName();
				}
			}

			moreButton = FindVisualChildByName(this.ColorPicker, "MoreButton") as ButtonBase;

			if (moreButton != null)
			{
				AutomationProperties.SetAutomationId(moreButton, "MoreButton");
			}

			FindAndGiveAutomationIdToVisualChild("ThirdDimensionSlider");
			FindAndGiveAutomationIdToVisualChild("AlphaSlider");
			FindAndGiveAutomationIdToVisualChild("MoreButtonLabel");
			FindAndGiveAutomationIdToVisualChild("ColorRepresentationComboBox");
			FindAndGiveAutomationIdToVisualChild("RedTextBox");
			FindAndGiveAutomationIdToVisualChild("RedLabel");
			FindAndGiveAutomationIdToVisualChild("GreenTextBox");
			FindAndGiveAutomationIdToVisualChild("GreenLabel");
			FindAndGiveAutomationIdToVisualChild("BlueTextBox");
			FindAndGiveAutomationIdToVisualChild("BlueLabel");
			FindAndGiveAutomationIdToVisualChild("HueTextBox");
			FindAndGiveAutomationIdToVisualChild("HueLabel");
			FindAndGiveAutomationIdToVisualChild("SaturationTextBox");
			FindAndGiveAutomationIdToVisualChild("SaturationLabel");
			FindAndGiveAutomationIdToVisualChild("ValueTextBox");
			FindAndGiveAutomationIdToVisualChild("ValueLabel");
			FindAndGiveAutomationIdToVisualChild("AlphaTextBox");
			FindAndGiveAutomationIdToVisualChild("AlphaLabel");
			FindAndGiveAutomationIdToVisualChild("HexTextBox");
		}

		private void SpectrumRectangleFillChanged(DependencyObject o, DependencyProperty p)
		{
			if (spectrumRectangle != null && spectrumRectangle.Fill != null)
			{
				this.ColorSpectrumLoadedCheckBox.IsChecked = true;
			}
		}

		private void PreviousColorRectangleFillChanged(DependencyObject o, DependencyProperty p)
		{
			if (previousColorRectangle != null)
			{
				SolidColorBrush previousColorBrush = previousColorRectangle.Fill as SolidColorBrush;

				if (previousColorBrush == null)
				{
					this.PreviousRedTextBlock.Text = "";
					this.PreviousGreenTextBlock.Text = "";
					this.PreviousBlueTextBlock.Text = "";
					this.PreviousAlphaTextBlock.Text = "";
				}
				else
				{
					Color previousColor = previousColorBrush.Color;

					this.PreviousRedTextBlock.Text = previousColor.R.ToString();
					this.PreviousGreenTextBlock.Text = previousColor.G.ToString();
					this.PreviousBlueTextBlock.Text = previousColor.B.ToString();
					this.PreviousAlphaTextBlock.Text = previousColor.A.ToString();
				}
			}
		}

		private void SelectionEllipseStrokeChanged(DependencyObject o, DependencyProperty p)
		{
			UpdateSelectionEllipseColor();
		}

		private void UpdateSelectionEllipseColor()
		{
			if (selectionEllipse != null)
			{
				SolidColorBrush selectionEllipseStrokeBrush = selectionEllipse.Stroke as SolidColorBrush;

				if (selectionEllipseStrokeBrush != null)
				{
					Color selectionEllipseColor = selectionEllipseStrokeBrush.Color;

					this.EllipseRedTextBlock.Text = selectionEllipseColor.R.ToString();
					this.EllipseGreenTextBlock.Text = selectionEllipseColor.G.ToString();
					this.EllipseBlueTextBlock.Text = selectionEllipseColor.B.ToString();
					this.EllipseAlphaTextBlock.Text = selectionEllipseColor.A.ToString();
				}
			}
		}

		private void SelectionEllipsePositionChanged(DependencyObject o, DependencyProperty p)
		{
			UpdateSelectionEllipsePosition();
		}

		private void UpdateSelectionEllipsePosition()
		{
			if (selectionEllipse != null)
			{
				double ellipseX = Canvas.GetLeft(selectionEllipsePanel) + selectionEllipsePanel.Width / 2;
				double ellipseY = Canvas.GetTop(selectionEllipsePanel) + selectionEllipsePanel.Height / 2;

				this.EllipseXTextBlock.Text = Math.Round(ellipseX).ToString();
				this.EllipseYTextBlock.Text = Math.Round(ellipseY).ToString();
			}
		}

		private void ColorNameToolTipContentChanged(DependencyObject o, DependencyProperty p)
		{
			UpdateSelectedColorName();
		}

		private void UpdateSelectedColorName()
		{
			this.SelectedColorNameTextBlock.Text = colorNameToolTip.Content as string ?? "";
		}

		private void ColorSpectrumImage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateHeightFromInputTarget();
		}

		private void UpdateHeightFromInputTarget()
		{
			if (colorSpectrumInputTarget != null)
			{
				this.WidthTextBlock.Text = colorSpectrumInputTarget.ActualWidth.ToString();
				this.HeightTextBlock.Text = colorSpectrumInputTarget.ActualHeight.ToString();
			}
		}

		private void ColorSpectrumShapeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox comboBox = (ComboBox)sender;

			switch (comboBox.SelectedIndex)
			{
				case 0:
					this.ColorPicker.ColorSpectrumShape = ColorSpectrumShape.Box;
					break;
				case 1:
					this.ColorPicker.ColorSpectrumShape = ColorSpectrumShape.Ring;
					break;
			}
		}

		private void ColorSpectrumComponentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox comboBox = (ComboBox)sender;

			switch (comboBox.SelectedIndex)
			{
				case 0:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.HueSaturation;
					break;
				case 1:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.HueValue;
					break;
				case 2:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.SaturationHue;
					break;
				case 3:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.SaturationValue;
					break;
				case 4:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.ValueHue;
					break;
				case 5:
					this.ColorPicker.ColorSpectrumComponents = ColorSpectrumComponents.ValueSaturation;
					break;
			}
		}

		private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			this.ColorFromEventRectangle.Fill = new SolidColorBrush(args.NewColor);

			this.RedTextBlock.Text = args.NewColor.R.ToString();
			this.GreenTextBlock.Text = args.NewColor.G.ToString();
			this.BlueTextBlock.Text = args.NewColor.B.ToString();
			this.AlphaTextBlock.Text = args.NewColor.A.ToString();

			this.OldRedTextBlock.Text = args.OldColor.R.ToString();
			this.OldGreenTextBlock.Text = args.OldColor.G.ToString();
			this.OldBlueTextBlock.Text = args.OldColor.B.ToString();
			this.OldAlphaTextBlock.Text = args.OldColor.A.ToString();
		}

		private void FindAndGiveAutomationIdToVisualChild(string childName)
		{
			DependencyObject obj = FindVisualChildByName(this.ColorPicker, childName);

			if (obj != null)
			{
				AutomationProperties.SetAutomationId(obj, childName);
			}
		}

		private void ThemeLightButton_Click(object sender, RoutedEventArgs e)
		{
			this.RequestedTheme = ElementTheme.Light;
			this.MoreButtonBackgroundTextBlock.Text = (moreButton.Background as SolidColorBrush).Color.ToString();
			this.MoreButtonForegroundTextBlock.Text = (moreButton.Foreground as SolidColorBrush).Color.ToString();
			this.MoreButtonBorderBrushTextBlock.Text = (moreButton.BorderBrush as SolidColorBrush).Color.ToString();
		}

		private void ThemeDarkButton_Click(object sender, RoutedEventArgs e)
		{
			this.RequestedTheme = ElementTheme.Dark;
			this.MoreButtonBackgroundTextBlock.Text = (moreButton.Background as SolidColorBrush).Color.ToString();
			this.MoreButtonForegroundTextBlock.Text = (moreButton.Foreground as SolidColorBrush).Color.ToString();
			this.MoreButtonBorderBrushTextBlock.Text = (moreButton.BorderBrush as SolidColorBrush).Color.ToString();
		}

		private void ColorWhiteButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.Color = Colors.White;
		}

		private void ColorRedButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.Color = Colors.Red;
		}

		private void ColorGreenButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.Color = Colors.Green;
		}

		private void ColorBlueButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.Color = Colors.Blue;
		}

		private void PreviousColorNullButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.PreviousColor = null;
		}

		private void PreviousColorRedButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.PreviousColor = Colors.Red;
		}

		private void PreviousColorGreenButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.PreviousColor = Colors.Green;
		}

		private void PreviousColorBlueButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.PreviousColor = Colors.Blue;
		}

		private void PreviousColorCurrentColorButton_Click(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.PreviousColor = this.ColorPicker.Color;
		}

		private void TestPage_GotFocus(object sender, RoutedEventArgs e)
		{
			FrameworkElement focusedElement = e.OriginalSource as FrameworkElement;

			if (focusedElement != null)
			{
				this.CurrentlyFocusedElementTextBlock.Text = string.IsNullOrEmpty(focusedElement.Name) ? "(an unnamed element)" : focusedElement.Name;
			}
			else
			{
				this.CurrentlyFocusedElementTextBlock.Text = "(nothing)";
			}
		}

		private void RTLCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.FlowDirection = FlowDirection.RightToLeft;
		}

		private void RTLCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			this.ColorPicker.FlowDirection = FlowDirection.LeftToRight;
		}

		private void OrientationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ColorPicker.Orientation = ((e.AddedItems[0] as ComboBoxItem).Content as string) == "Horizontal" ? Orientation.Horizontal : Orientation.Vertical;
		}

		// Uno TODO: Move this out into a helper class
		public static DependencyObject FindVisualChildByName(FrameworkElement parent, string name)
		{
			if (parent.Name == name)
			{
				return parent;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < childrenCount; i++)
			{
				FrameworkElement childAsFE = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;

				if (childAsFE != null)
				{
					DependencyObject result = FindVisualChildByName(childAsFE, name);

					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}
	}

	public partial class MyColorSpectrum : ColorSpectrum
	{
		protected override void OnApplyTemplate()
		{
			// add an override just to exercise the override chaining.
			base.OnApplyTemplate();
		}
	}
}
