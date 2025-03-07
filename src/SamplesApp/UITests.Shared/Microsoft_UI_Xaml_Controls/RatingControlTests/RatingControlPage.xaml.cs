// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using System.Numerics;
using Windows.UI;
using Windows.Foundation.Metadata;
using RatingControl = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingControl;
using RatingItemImageInfo = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingItemImageInfo;
using RatingItemFontInfo = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingItemFontInfo;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.RatingControlTests
{
	[Sample("RatingControl", "MUX")]
	public sealed partial class RatingControlPage : Page
	{
		UIElement _secondTextBlockUI;
		DispatcherTimer _dt;

		SolidColorBrush _tomato = new SolidColorBrush(Colors.Tomato);
		SolidColorBrush _aqua = new SolidColorBrush(Colors.Aqua);

		public RatingControlPage()
		{
			this.InitializeComponent();

			TestRatingControl.ValueChanged += TestRatingControl_ValueChanged;

			RatingDarkTheme.PlaceholderValue = 1.5;

			MyRatingReadOnlyTextBlock.Text = "2.2";
			MyRatingReadOnlyWithPlaceholder.PlaceholderValue = 3.3;

			DisabledWithValue.Value = 3;
			DisabledWithPlaceholderValue.PlaceholderValue = 3;

			CustomImages.Value = 3.0;
			CustomImages.PlaceholderValue = 1.5;

			var imageInfo = new RatingItemImageInfo();
			imageInfo.Image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_set.png"));
			imageInfo.UnsetImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_unset.png")); ;
			imageInfo.PlaceholderImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_placeholder.png")); ;
			imageInfo.DisabledImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_disabled.png")); ;
			imageInfo.PointerOverImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_mouseoverset.png")); ;
			imageInfo.PointerOverPlaceholderImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_mouseoverplaceholder.png"));

			CustomImagesTwo.ItemInfo = imageInfo;
			CustomImagesTwo.Value = 3.0;
			CustomImagesTwo.PlaceholderValue = 4.25;

			PointerOverPlaceholderFallbackRating.AddHandler(RatingControl.PointerMovedEvent, new PointerEventHandler(PointerOverPlaceholderFallbackRating_PointerMoved), true);
			PointerOverFallbackRating.AddHandler(RatingControl.PointerMovedEvent, new PointerEventHandler(PointerOverFallbackRating_PointerMoved), true);

			PointerOverPlaceholderImageFallbackRating.AddHandler(RatingControl.PointerMovedEvent, new PointerEventHandler(PointerOverPlaceholderImageFallbackRating_PointerMoved), true);
			PointerOverImageFallbackRating.AddHandler(RatingControl.PointerMovedEvent, new PointerEventHandler(PointerOverImageFallbackRating_PointerMoved), true);

			ColorFlipButton.Foreground = _tomato;

			RatingBindingSample.DataContext = CaptionStringBox;
			BindingRatingCaption.DataContext = ColorFlipButton;

			//TODO: Uno Platform - WinUI wraps controls in a "TestFrame" which has some "ambient" features, including the view scaling checkbox
			//var testFrame = Window.Current.Content as TestFrame;
			//DependencyObject checkBox = SearchVisualTree(testFrame, "ViewScalingCheckBox");
			//CheckBox cb = checkBox as CheckBox;
			//FrameDetails.Text = Window.Current.Bounds.ToString() + " " + cb.IsChecked.ToString();

#if !HAS_UNO
			if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Controls.RatingControl"))
			{
				var wuxcRatingControl = new Windows.UI.Xaml.Controls.RatingControl();
				wuxcRatingControl.Name = "WUXC RatingControl";
				wuxcRatingControl.Caption = "WUXC RatingControl";
				AutomationProperties.SetAutomationId(wuxcRatingControl, "wuxcRatingControl");
				this.mainStackPanel.Children.Add(wuxcRatingControl);
			}
#endif
		}

		DependencyObject SearchVisualTree(DependencyObject root, string name)
		{
			int size = VisualTreeHelper.GetChildrenCount(root);
			DependencyObject child = null;

			for (int i = 0; i < size && child == null; i++)
			{
				DependencyObject depObj = VisualTreeHelper.GetChild(root, i);
				FrameworkElement fe = depObj as FrameworkElement;

				if (fe.Name.Equals(name))
				{
					child = fe;
				}
				else
				{
					child = SearchVisualTree(fe, name);
				}
			}

			return child;
		}

		// Uno specific: Unlike WinUI, we unsubscribe on Unloaded because we don't get OnNavigatedFrom.
		// See point #2 in https://github.com/unoplatform/uno/issues/15059#issuecomment-1891551501
		private void TestRatingControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_dt.Tick -= DispatcherTimer_Tick; // prevent leaks since the dispatcher holds a pointer to this
		}

		private void TestRatingControl_ValueChanged(RatingControl sender, object args)
		{
			if (TestRatingControl.Value == -1)
			{
				if (TestRatingControl.PlaceholderValue == -1)
				{
					TestTextBlockControl.Text = "!";
				}
				else
				{
					TestTextBlockControl.Text = "!" + TestRatingControl.PlaceholderValue.ToString();
				}
			}
			else
			{
				TestTextBlockControl.Text = sender.Value.ToString(); // Having both ways of referring to the control to make sure both work
			}
		}

		private void ValueChangeInMarkup_ValueChanged(RatingControl sender, object args)
		{
			if (ValueChangeInMarkup.Value == -1)
			{
				ValueChangeInMarkupText.Text = "!!" + ValueChangeInMarkup.PlaceholderValue.ToString();
			}
			else
			{
				ValueChangeInMarkupText.Text = ValueChangeInMarkup.Value.ToString();
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.FlowDirection = Frame.FlowDirection == FlowDirection.LeftToRight ?
				FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		}

		private void TestRatingControlButton_Click(object sender, RoutedEventArgs e)
		{
			TestRatingControl.PlaceholderValue = -1;
		}

		private void MyRatingIsClearEnabled_ValueChanged(RatingControl sender, object args)
		{
			if (sender.Value == -1)
			{
				MyRatingIsClearEnabledText.Text = "!" + sender.PlaceholderValue.ToString();
			}
			else
			{
				MyRatingIsClearEnabledText.Text = sender.Value.ToString();
			}
		}

		private void CollapsedRatingControlButton_Click(object sender, RoutedEventArgs e)
		{
			CollapsedRatingControl.Value = 3.3;
		}

		private void RatingXBindSampleButton_Click(object sender, RoutedEventArgs e)
		{
			RatingXBindSampleText.Text = RatingXBindSample.Caption;
		}

		private void RatingBindingSampleButton_Click(object sender, RoutedEventArgs e)
		{
			RatingBindingSampleText.Text = RatingBindingSample.Caption;
		}

		private void MaxRating9Unset_ValueChanged(RatingControl sender, object args)
		{
			if (sender.Value == -1)
			{
				MaxRating9UnsetTextBlock.Text = "!!" + sender.PlaceholderValue.ToString();
			}
			else
			{
				MaxRating9UnsetTextBlock.Text = sender.Value.ToString();
			}
		}

		private void MakeTheAboveRatingSquares_Click(object sender, RoutedEventArgs e)
		{
			MakeMeSquares.FontFamily = new FontFamily("Times New Roman");
		}

		private void MyRatingReadOnly_ValueChanged(RatingControl sender, object args)
		{
			MyRatingReadOnlyTextBlock.Text = sender.Value.ToString();
		}

		private void CustomImages_Loaded(object sender, RoutedEventArgs e)
		{
			DependencyObject obj = FindVisualChildByName(CustomImages, "RatingBackgroundStackPanel");
			var child = VisualTreeHelper.GetChild(obj, 0);
			AutomationProperties.SetAutomationId(child, "CustomImages_FirstImageItem");
			AutomationProperties.SetAccessibilityView(child, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Control);

			CustomImagesLoadedCheckBox.IsChecked = true;
		}

		private void ChangeCustomImagesTwoType_Click(object sender, RoutedEventArgs e)
		{
			if (CustomImagesTwo.ItemInfo is RatingItemImageInfo)
			{
				var rifi = new RatingItemFontInfo();
				rifi.Glyph = "\uEB52";
				rifi.UnsetGlyph = "\uEB52";
				rifi.PointerOverGlyph = "\uEB52";
				rifi.PointerOverPlaceholderGlyph = "\uEB52";
				rifi.DisabledGlyph = "\uEB52";

				CustomImagesTwo.ItemInfo = rifi;

				DependencyObject obj = FindVisualChildByName(CustomImagesTwo, "RatingBackgroundStackPanel");
				var child = VisualTreeHelper.GetChild(obj, 0);
				AutomationProperties.SetAutomationId(child, "CustomImagesTwo_FirstTextItem");
				AutomationProperties.SetAccessibilityView(child, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Control);

				CustomImagesTwoLoadedStageTwoCheckBox.IsChecked = true;
			}
			else
			{
				var imageInfo = new RatingItemImageInfo();
				imageInfo.Image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_set.png"));
				imageInfo.UnsetImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_unset.png")); ;
				imageInfo.PlaceholderImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_placeholder.png")); ;
				imageInfo.DisabledImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_disabled.png")); ;
				imageInfo.PointerOverImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_mouseoverset.png")); ;
				imageInfo.PointerOverPlaceholderImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/Assets/rating_mouseoverplaceholder.png"));

				CustomImagesTwo.ItemInfo = imageInfo;

				DependencyObject obj = FindVisualChildByName(CustomImagesTwo, "RatingBackgroundStackPanel");
				var child = VisualTreeHelper.GetChild(obj, 0);
				AutomationProperties.SetAutomationId(child, "CustomImagesTwo_FirstImageItem_Again");
				AutomationProperties.SetAccessibilityView(child, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Control);

				CustomImagesTwoLoadedStageThreeCheckBox.IsChecked = true;
			}
		}

		private void CustomImagesTwo_Loaded(object sender, RoutedEventArgs e)
		{
			DependencyObject obj = FindVisualChildByName(CustomImagesTwo, "RatingBackgroundStackPanel");
			var child = VisualTreeHelper.GetChild(obj, 0);
			AutomationProperties.SetAutomationId(child, "CustomImagesTwo_FirstImageItem");
			AutomationProperties.SetAccessibilityView(child, Windows.UI.Xaml.Automation.Peers.AccessibilityView.Control);

			CustomImagesTwoLoadedStageOneCheckBox.IsChecked = true;
		}

		private void TestRatingControl_Loaded(object sender, RoutedEventArgs e)
		{
			_dt = new DispatcherTimer();
			_dt.Interval = TimeSpan.FromSeconds(0.1);
			_dt.Start();
			_dt.Tick += DispatcherTimer_Tick;

			DependencyObject obj = FindVisualChildByName(TestRatingControl, "RatingBackgroundStackPanel");
			UIElement ui = obj as UIElement;
			DependencyObject child = VisualTreeHelper.GetChild(obj, 1);
			TextBlock tb = child as TextBlock;
			this._secondTextBlockUI = tb;

			//SetupAnimatedValuesSpy();
			//SpyAnimatedValues();
		}

		private void DispatcherTimer_Tick(object sender, object e)
		{
			SpyAnimatedValues();
		}

		// Composition property spy stuff below:
		private CompositionPropertySet AnimatedValuesSpy
		{
			get;
			set;
		}

		private ExpressionAnimation ScaleAnimation
		{
			get;
			set;
		}

		private bool IsRenderingHooked
		{
			get;
			set;
		}

		private uint UIThreadTicksForValuesSpy
		{
			get;
			set;
		}

		private void SetupAnimatedValuesSpy()
		{
			StopAnimatedValuesSpy();

			this.AnimatedValuesSpy = null;
			this.ScaleAnimation = null;

			if (this._secondTextBlockUI != null)
			{
				const string visualScaleTargetedPropertyName = "visual.Scale";
				UIElement ppChild = this._secondTextBlockUI;
				Visual visualPPChild = ElementCompositionPreview.GetElementVisual(ppChild);

				Compositor compositor = visualPPChild.Compositor;

				this.AnimatedValuesSpy = compositor.CreatePropertySet();

				this.AnimatedValuesSpy.InsertVector3("Scale", new Vector3(0.0f, 0.0f, 0.0f));

				this.ScaleAnimation = compositor.CreateExpressionAnimation(visualScaleTargetedPropertyName);
				this.ScaleAnimation.SetReferenceParameter("visual", visualPPChild);

				CheckSpyingTicksRequirement();

				TickForValuesSpy();
			}
			else
			{
				ResetSpyOutput();
			}
		}

		private void StartAnimatedValuesSpy()
		{
			if (this.AnimatedValuesSpy != null)
			{
				this.AnimatedValuesSpy.StartAnimation("Scale", this.ScaleAnimation);
			}
		}

		private void StopAnimatedValuesSpy()
		{
			if (this.AnimatedValuesSpy != null)
			{
				this.AnimatedValuesSpy.StopAnimation("Scale");
			}
		}

		private void SpyAnimatedValues()
		{
			if (this.AnimatedValuesSpy != null)
			{
				StopAnimatedValuesSpy();

				Vector3 scale;
				CompositionGetValueStatus status = this.AnimatedValuesSpy.TryGetVector3("Scale", out scale);
				if (CompositionGetValueStatus.Succeeded == status)
				{
					this.ScaleTextX.Text = scale.X.ToString();
					this.ScaleTextY.Text = scale.Y.ToString();
				}
				else
				{
					this.ScaleTextX.Text = this.ScaleTextY.Text = "status=" + status.ToString();
				}

				StartAnimatedValuesSpy();

				// System.Diagnostics.Debug.WriteLine("Spied values: " + this.ScaleTextX.Text);
			}
		}

		private void ResetSpyOutput()
		{
			this.ScaleTextX.Text = this.ScaleTextY.Text = "0 - reset";
		}

		private void TickForValuesSpy()
		{
			this.UIThreadTicksForValuesSpy = 6;
			CheckSpyingTicksRequirement();
		}

		private void CheckSpyingTicksRequirement()
		{
			if (this._secondTextBlockUI != null &&
				(this.UIThreadTicksForValuesSpy > 0) && this.AnimatedValuesSpy != null)
			{
				if (!this.IsRenderingHooked)
				{
					this.IsRenderingHooked = true;
					Windows.UI.Xaml.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
				}
			}
			else
			{
				if (this.IsRenderingHooked)
				{
					Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
					this.IsRenderingHooked = false;
				}
			}
		}

		private void CompositionTarget_Rendering(object sender, object e)
		{
			if (this.UIThreadTicksForValuesSpy > 0)
			{
				this.UIThreadTicksForValuesSpy--;
			}
			CheckSpyingTicksRequirement();
			SpyAnimatedValues();
		}

		void PerformGlyphVerification(RatingControl rc, String expectedGlyph, TextBlock resultTb, String stackPanelName, SolidColorBrush expectedColor = null)
		{
			ContentPresenter presenter = FindVisualChildByName(rc, "ForegroundContentPresenter") as ContentPresenter;

			DependencyObject obj = FindVisualChildByName(rc, stackPanelName);
			var child = VisualTreeHelper.GetChild(obj, 0);
			TextBlock tb = child as TextBlock;

			String glyph = tb.Text;
			if (glyph.Equals(expectedGlyph))
			{
				resultTb.Text = "+";
				// Verify colour to make sure we don't accidentally take the non-pointer over as a false-positive
				if (expectedColor == null || presenter.Foreground.Equals(expectedColor))
				{
					resultTb.Text = "+";
				}
				else
				{
					resultTb.Text = "/";
				}
			}
			else
			{
				resultTb.Text = "-";
			}
		}

		void PerformImageVerification(RatingControl rc, String expectedUri, TextBlock resultTb, String stackPanelName, SolidColorBrush expectedColor = null)
		{
			ContentPresenter presenter = FindVisualChildByName(rc, "ForegroundContentPresenter") as ContentPresenter;

			DependencyObject obj = FindVisualChildByName(rc, stackPanelName);
			var child = VisualTreeHelper.GetChild(obj, 0);
			Image image = child as Image;
			var source = image.Source;
			Windows.UI.Xaml.Media.Imaging.BitmapImage bitImage = source as Windows.UI.Xaml.Media.Imaging.BitmapImage;

			String uri = bitImage.UriSource.ToString();
			if (uri.Equals(expectedUri))
			{
				resultTb.Text = "+";
				// Verify colour to make sure we don't accidentally take the non-pointer over as a false-positive
				// The colours aren't used, but the state is there so we can still check.
				if (expectedColor == null || presenter.Foreground.Equals(expectedColor))
				{
					resultTb.Text = "+";
				}
				else
				{
					resultTb.Text = "/";
				}
			}
			else
			{
				resultTb.Text = "-";
			}
		}

		SolidColorBrush ThemeResourceNameToColorBrush(String themeResource)
		{
			return Application.Current.Resources[themeResource] as SolidColorBrush;
		}

		private void UnsetFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformGlyphVerification(UnsetFallbackRating, "\uE00B" /* full heart */, UnsetFallbackTextBlock, "RatingBackgroundStackPanel");
		}

		private void PlaceholderFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformGlyphVerification(PlaceholderFallbackRating, "\uE909" /* world/globe */, PlaceholderFallbackTextBlock, "RatingForegroundStackPanel");
		}

		private void DisabledFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformGlyphVerification(DisabledFallbackRating, "\uE8DD" /* world/globe */, DisabledFallbackTextBlock, "RatingForegroundStackPanel");
		}

		private void PointerOverPlaceholderFallbackRating_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!PointerOverPlaceholderFallbackTextBlock.Text.Equals("+"))
			{
				// Have to make sure this doesn't trigger before the PointerOver
				PerformGlyphVerification(PointerOverPlaceholderFallbackRating, "\uEBAA", PointerOverPlaceholderFallbackTextBlock,
					"RatingForegroundStackPanel", ThemeResourceNameToColorBrush("RatingControlPointerOverPlaceholderForeground"));
			}
		}

		private void PointerOverFallbackRating_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!PointerOverFallbackTextBlock.Text.Equals("+"))
			{
				// Have to make sure this doesn't trigger before the PointerOver
				PerformGlyphVerification(PointerOverFallbackRating, "\uE8CA", PointerOverFallbackTextBlock,
					"RatingForegroundStackPanel", ThemeResourceNameToColorBrush("RatingControlSelectedForeground"));
			}
		}

		private void NoFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformGlyphVerification(NoFallbackRating, "" /* glyph should fall back to nothing */, NoFallbackTextBlock, "RatingForegroundStackPanel");
		}

		private void UnsetImageFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformImageVerification(UnsetImageFallbackRating, "ms-appx:/Assets/rating_set.png", UnsetImageFallbackTextBlock, "RatingBackgroundStackPanel");
		}

		private void PlaceholderImageFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformImageVerification(PlaceholderImageFallbackRating, "ms-appx:/Assets/rating_unset.png", PlaceholderImageFallbackTextBlock, "RatingBackgroundStackPanel");
		}

		private void DisabledImageFallbackRating_Loaded(object sender, RoutedEventArgs e)
		{
			PerformImageVerification(DisabledImageFallbackRating, "ms-appx:/Assets/rating_placeholder.png" /* world/globe */, DisabledImageFallbackTextBlock, "RatingForegroundStackPanel");
		}

		private void PointerOverPlaceholderImageFallbackRating_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!PointerOverPlaceholderImageFallbackTextBlock.Text.Equals("+"))
			{
				// Have to make sure this doesn't trigger before the PointerOver
				PerformImageVerification(PointerOverPlaceholderImageFallbackRating, "ms-appx:/Assets/rating_disabled.png", PointerOverPlaceholderImageFallbackTextBlock,
					"RatingForegroundStackPanel", ThemeResourceNameToColorBrush("RatingControlPointerOverPlaceholderForeground"));
			}
		}

		private void PointerOverImageFallbackRating_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!PointerOverImageFallbackTextBlock.Text.Equals("+"))
			{
				// Have to make sure this doesn't trigger before the PointerOver
				PerformImageVerification(PointerOverImageFallbackRating, "ms-appx:/Assets/rating_mouseoverplaceholder.png", PointerOverImageFallbackTextBlock,
					"RatingForegroundStackPanel", ThemeResourceNameToColorBrush("RatingControlSelectedForeground"));
			}
		}

		private void ColorFlipButton_Click(object sender, RoutedEventArgs e)
		{
			if (ColorFlipButton.Foreground.Equals(_tomato))
			{
				ColorFlipButton.Foreground = _aqua;
			}
			else
			{
				ColorFlipButton.Foreground = _tomato;
			}
		}

		private void MagicDisengager_ValueChanged(RatingControl sender, object args)
		{
			if (sender.Value == 3.0)
			{
				sender.RemoveFocusEngagement();
			}

			if (sender.Value == -1)
			{
				MagicDisengagerTextBlock.Text = "null";
			}
			else
			{
				MagicDisengagerTextBlock.Text = sender.Value.ToString();
			}
		}


		//TODO: Uno specific - move to base WinUI TestPage
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
}
