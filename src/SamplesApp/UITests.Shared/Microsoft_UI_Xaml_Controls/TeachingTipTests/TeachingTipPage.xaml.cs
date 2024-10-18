using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using TeachingTip = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTip;
using TeachingTipClosedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTipClosedEventArgs;
using TeachingTipClosingEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTipClosingEventArgs;
#if HAS_UNO // Private types are internal and available in Uno only.
using TeachingTipTestHooks = Microsoft.UI.Private.Controls.TeachingTipTestHooks;
#endif
using TeachingTipTailVisibility = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTipTailVisibility;
using TeachingTipHeroContentPlacementMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTipHeroContentPlacementMode;
using TeachingTipPlacementMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTipPlacementMode;
using SymbolIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SymbolIconSource;
using Uno.UI.Samples.Controls;
using Popup = Windows.UI.Xaml.Controls.Primitives.Popup;
using MUXControlsTestApp.Utilities;

#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast

namespace UITests.Microsoft_UI_Xaml_Controls.TeachingTipTests
{
	enum TipLocation
	{
		VisualTree = 0,
		Resources = 1
	}

	[Sample("TeachingTip", "WinUI")]
	public sealed partial class TeachingTipPage : MUXTestPage, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		Deferral deferral;
		DispatcherTimer timer;
		DispatcherTimer showTimer;
		Popup testWindowBounds;
		Popup testScreenBounds;
		TipLocation tipLocation = TipLocation.VisualTree;
		FrameworkElement TeachingTipInResourcesRoot;
		FrameworkElement TeachingTipInVisualTreeRoot;
		CheckBox CurrentCancelClosesCheckBox;

		public TeachingTipPage()
		{
			this.InitializeComponent();
#if HAS_UNO
			TeachingTipTestHooks.IdleStatusChanged += TeachingTipTestHooks_IdleStatusChanged;
			TeachingTipTestHooks.OpenedStatusChanged += TeachingTipTestHooks_OpenedStatusChanged;
			TeachingTipTestHooks.EffectivePlacementChanged += TeachingTipTestHooks_EffectivePlacementChanged;
			TeachingTipTestHooks.EffectiveHeroContentPlacementChanged += TeachingTipTestHooks_EffectiveHeroContentPlacementChanged;
			TeachingTipTestHooks.OffsetChanged += TeachingTipTestHooks_OffsetChanged;
			TeachingTipTestHooks.TitleVisibilityChanged += TeachingTipTestHooks_TitleVisibilityChanged;
			TeachingTipTestHooks.SubtitleVisibilityChanged += TeachingTipTestHooks_SubtitleVisibilityChanged;
#endif
			this.TeachingTipInVisualTree.Closed += TeachingTipInVisualTree_Closed;
			this.TeachingTipInResources.Closed += TeachingTipInResources_Closed;
			this.TeachingTipInResourcesOnEdge.Closed += TeachingTipInResourcesOnEdge_Closed;
			this.ContentScrollViewer.ViewChanged += ContentScrollViewer_ViewChanged;
		}

		private void TeachingTipInResources_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
		{
			if (TeachingTipInResourcesRoot != null)
			{
				TeachingTipInResourcesRoot.SizeChanged -= TeachingTip_SizeChanged;
			}
		}
		private void TeachingTipInResourcesOnEdge_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
		{
			if (TeachingTipInResourcesOnEdge != null)
			{
				TeachingTipInResourcesOnEdge.SizeChanged -= TeachingTip_SizeChanged;
			}
		}

		private void TeachingTipInVisualTree_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
		{
			if (TeachingTipInVisualTreeRoot != null)
			{
				TeachingTipInVisualTreeRoot.SizeChanged -= TeachingTip_SizeChanged;
			}
		}

		protected
#if HAS_UNO
			internal
#endif
			override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (testWindowBounds != null && testWindowBounds.IsOpen)
			{
				testWindowBounds.IsOpen = false;
			}
			if (testScreenBounds != null && testScreenBounds.IsOpen)
			{
				testScreenBounds.IsOpen = false;
			}
			base.OnNavigatedFrom(e);
		}

		private void TeachingTip_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.TipHeightTextBlock.Text = ((FrameworkElement)sender).ActualHeight.ToString();
			this.TipWidthTextBlock.Text = ((FrameworkElement)sender).ActualWidth.ToString();
			NotifyPropertyChanged("ActionButton");
		}

		private void TeachingTipTestHooks_OffsetChanged(TeachingTip sender, object args)
		{
			if (sender == getTeachingTip())
			{
#if HAS_UNO
				this.PopupVerticalOffsetTextBlock.Text = TeachingTipTestHooks.GetVerticalOffset(sender).ToString();
				this.PopupHorizontalOffsetTextBlock.Text = TeachingTipTestHooks.GetHorizontalOffset(sender).ToString();
#endif
			}
		}

		private void TeachingTipTestHooks_TitleVisibilityChanged(TeachingTip sender, object args)
		{
			if (sender == getTeachingTip())
			{
#if HAS_UNO
				this.TitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetTitleVisibility(sender).ToString();
#endif
			}
		}

		private void TeachingTipTestHooks_SubtitleVisibilityChanged(TeachingTip sender, object args)
		{
			if (sender == getTeachingTip())
			{
#if HAS_UNO
				this.SubtitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetSubtitleVisibility(sender).ToString();
#endif
			}
		}

		private void TeachingTipTestHooks_EffectiveHeroContentPlacementChanged(TeachingTip sender, object args)
		{
			if (sender == getTeachingTip())
			{
#if HAS_UNO
				var placement = TeachingTipTestHooks.GetEffectiveHeroContentPlacement(sender);
				this.EffectiveHeroContentPlacementTextBlock.Text = placement.ToString();
#endif
			}
		}

		private void TeachingTipTestHooks_EffectivePlacementChanged(TeachingTip sender, object args)
		{
			if (sender == getTeachingTip())
			{
#if HAS_UNO
				var placement = TeachingTipTestHooks.GetEffectivePlacement(sender);
				this.EffectivePlacementTextBlock.Text = placement.ToString();
#endif
			}
		}

		private void ContentScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			this.ScrollViewerOffsetTextBox.Text = this.ContentScrollViewer.VerticalOffset.ToString();
			if (e.IsIntermediate)
			{
				this.ScrollViewerStateTextBox.Text = "Scrolling";
			}
			else
			{
				this.ScrollViewerStateTextBox.Text = "Idle";
			}
			OnGetTargetBoundsButtonClicked(null, null);
		}

		private void TeachingTipTestHooks_OpenedStatusChanged(TeachingTip sender, object args)
		{
			if (this.TeachingTipInResources.IsOpen ||
				this.TeachingTipInVisualTree.IsOpen)
			{
				this.IsOpenCheckBox.IsChecked = true;
			}
			else
			{
				this.IsOpenCheckBox.IsChecked = false;
			}
		}

		private void TeachingTipTestHooks_IdleStatusChanged(TeachingTip sender, object args)
		{
#if HAS_UNO
			if (TeachingTipTestHooks.GetIsIdle(this.TeachingTipInResources) &&
				TeachingTipTestHooks.GetIsIdle(this.TeachingTipInVisualTree))
			{
				this.IsIdleCheckBox.IsChecked = true;
			}
			else
			{
				this.IsIdleCheckBox.IsChecked = false;
			}
#endif
		}

		public void OnSetIconButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.IconComboBox.SelectedItem == IconPeople)
			{
				SymbolIconSource symbolIconSource = new SymbolIconSource();
				symbolIconSource.Symbol = Symbol.People;
				getTeachingTip().IconSource = symbolIconSource;

			}
			else
			{
				getTeachingTip().IconSource = null;
			}
		}

		public void OnSetTipLocationButton(object sender, RoutedEventArgs args)
		{
			if (this.TipLocationComboBox.SelectedItem == TipInVisualTree)
			{
				if (tipLocation != TipLocation.VisualTree)
				{
					TeachingTipInResources.IsOpen = false;
					tipLocation = TipLocation.VisualTree;
				}
				tipLocation = TipLocation.VisualTree;
				AutomationNameComboBox.SelectedItem = AutomationNameVisualTree;
			}
			else
			{
				if (tipLocation != TipLocation.Resources)
				{
					TeachingTipInVisualTree.IsOpen = false;
					tipLocation = TipLocation.Resources;
				}
				tipLocation = TipLocation.Resources;
				AutomationNameComboBox.SelectedItem = AutomationNameResources;
			}
			OnSetAutomationNameButtonClicked(null, null);
		}

		public void OnSetHeroContentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.HeroContentComboBox.SelectedItem == HeroContentRedSquare)
			{
				Grid grid = new Grid();
				grid.MinHeight = 200;
				grid.MinWidth = 200;
				grid.Background = new SolidColorBrush(Colors.Red);
				getTeachingTip().HeroContent = grid;
			}
			else if (this.HeroContentComboBox.SelectedItem == HeroContentBlueSquare)
			{
				Grid grid = new Grid();
				grid.MinHeight = 200;
				grid.MinWidth = 200;
				grid.Background = new SolidColorBrush(Colors.Blue);
				getTeachingTip().HeroContent = grid;
			}
			else if (this.HeroContentComboBox.SelectedItem == HeroContentImage)
			{
				Image image = new Image();
				BitmapImage bitmapImage = new BitmapImage();
				image.Width = bitmapImage.DecodePixelWidth = 300;
				bitmapImage.UriSource = new Uri("ms-appx:///Assets/ingredient1.png");
				image.Source = bitmapImage;
				getTeachingTip().HeroContent = image;
			}
			else if (this.HeroContentComboBox.SelectedItem == HeroContentAutoSave)
			{
				Image image = new Image();
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.UriSource = new Uri("ms-appx:///Assets/AutoSave.png");
				image.Source = bitmapImage;
				getTeachingTip().HeroContent = image;
			}
			else
			{
				getTeachingTip().HeroContent = null;
			}
		}

		public void OnSetContentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.ContentComboBox.SelectedItem == ContentRedSquare)
			{
				Grid grid = new Grid();
				grid.Background = new SolidColorBrush(Colors.Red);
				getTeachingTip().Content = grid;
			}
			else if (this.ContentComboBox.SelectedItem == ContentBlueSquare)
			{
				Grid grid = new Grid();
				grid.Background = new SolidColorBrush(Colors.Blue);
				getTeachingTip().Content = grid;
			}
			else if (this.ContentComboBox.SelectedItem == ContentImage)
			{
				Image image = new Image();
				BitmapImage bitmapImage = new BitmapImage();
				image.Width = bitmapImage.DecodePixelWidth = 300;
				bitmapImage.UriSource = new Uri("ms-appx:///Assets/ingredient1.png");
				image.Source = bitmapImage;
				getTeachingTip().Content = image;
			}
			else if (this.ContentComboBox.SelectedItem == ContentShort)
			{
				TextBlock textBlock = new TextBlock();
				textBlock.Text = "This is shorter content.";
				getTeachingTip().Content = textBlock;
			}
			else if (this.ContentComboBox.SelectedItem == ContentLong)
			{
				TextBlock textBlock = new TextBlock();
				textBlock.Text = "This is longer content. This is longer content. This is longer content. This is longer content. " +
					"This is longer content. This is longer content.This is longer content. This is longer content." +
					"This is longer content.This is longer content.This is longer content. This is longer content." +
					"This is longer content.This is longer content.This is longer content.This is longer content." +
					"This is longer content.This is longer content.This is longer content.This is longer content." +
					"This is longer content.This is longer content.This is longer content.This is longer content." +
					"This is longer content.This is longer content.This is longer content.This is longer content." +
					"This is longer content.This is longer content.This is longer content.This is longer content." +
					"This is longer content.This is longer content.";
				textBlock.TextWrapping = TextWrapping.WrapWholeWords;
				getTeachingTip().Content = textBlock;
			}
			else if (this.ContentComboBox.SelectedItem == ContentAutoSave)
			{
				Image image = new Image();
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.UriSource = new Uri("ms-appx:///Assets/AutoSave.png");
				image.Source = bitmapImage;
				getTeachingTip().Content = image;
			}
			else
			{
				getTeachingTip().Content = null;
			}
		}

		public void OnSetTitleButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.TitleComboBox.SelectedItem == TitleNo)
			{
				getTeachingTip().Title = "";
			}
			else if (this.TitleComboBox.SelectedItem == TitleSmall)
			{
				getTeachingTip().Title = "Short title.";
			}
			else
			{
				getTeachingTip().Title = "This is a much longer title that might cause some issues if we don't do the right thing...";
			}
		}

		public void OnSetSubtitleButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.SubtitleComboBox.SelectedItem == SubtitleNo)
			{
				getTeachingTip().Subtitle = "";
			}
			else if (this.SubtitleComboBox.SelectedItem == SubtitleSmall)
			{
				getTeachingTip().Subtitle = "Short Subtitle.";
			}
			else
			{
				getTeachingTip().Subtitle = "This is a much longer subtitle that might cause some issues if we don't do the right thing..." +
					"This is a much longer subtitle that might cause some issues if we don't do the right thing...";
			}
		}

		public void OnSetActionButtonContentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.ActionButtonContentComboBox.SelectedItem == ActionButtonContentNo)
			{
				getTeachingTip().ActionButtonContent = "";
			}
			else if (this.ActionButtonContentComboBox.SelectedItem == ActionButtonContentSmall)
			{
				getTeachingTip().ActionButtonContent = "A:Short Text.";
			}
			else if (this.ActionButtonContentComboBox.SelectedItem == ActionButtonContentLong)
			{
				getTeachingTip().ActionButtonContent = "A:This is a much longer button text that might cause some issues if we don't do the right thing...";
			}
			else
			{
				var button = new Button();
				button.Content = "A:Button in a Button!";
				getTeachingTip().ActionButtonContent = button;
			}
			NotifyPropertyChanged("ActionButton");
		}

		public void OnSetCloseButtonContentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.CloseButtonContentComboBox.SelectedItem == CloseButtonContentNo)
			{
				getTeachingTip().CloseButtonContent = "";
			}
			else if (this.CloseButtonContentComboBox.SelectedItem == CloseButtonContentSmall)
			{
				getTeachingTip().CloseButtonContent = "C:Short Text.";
			}
			else if (this.CloseButtonContentComboBox.SelectedItem == CloseButtonContentLong)
			{
				getTeachingTip().CloseButtonContent = "C:This is a much longer button text that might cause some issues if we don't do the right thing...";
			}
			else
			{
				var button = new Button();
				button.Content = "C:Button in a Button!";
				getTeachingTip().CloseButtonContent = button;
			}
		}

		public void OnSetBleeingImagePlacementButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.HeroContentPlacementComboBox.SelectedItem == HeroContentPlacementAuto)
			{
				getTeachingTip().HeroContentPlacement = TeachingTipHeroContentPlacementMode.Auto;
			}
			else if (this.HeroContentPlacementComboBox.SelectedItem == HeroContentPlacementTop)
			{
				getTeachingTip().HeroContentPlacement = TeachingTipHeroContentPlacementMode.Top;
			}
			else
			{
				getTeachingTip().HeroContentPlacement = TeachingTipHeroContentPlacementMode.Bottom;
			}
		}
		public void OnGetTargetBoundsButtonClicked(object sender, RoutedEventArgs args)
		{
			var bounds = this.targetButton.TransformToVisual(null).TransformBounds(new Rect(0.0,
				0.0,
				this.targetButton.ActualWidth,
				this.targetButton.ActualHeight));

			this.TargetXOffsetTextBlock.Text = bounds.X.ToString();
			this.TargetYOffsetTextBlock.Text = bounds.Y.ToString();
			this.TargetWidthTextBlock.Text = bounds.Width.ToString();
			this.TargetHeightTextBlock.Text = bounds.Height.ToString();
		}

		public void OnSetScrollViewerOffsetButtonClicked(object sender, RoutedEventArgs args)
		{
			this.ContentScrollViewer.ChangeView(0, double.Parse(this.ScrollViewerOffsetTextBox.Text), 1);
		}

		public void OnBringTargetIntoViewButtonClicked(object sender, RoutedEventArgs args)
		{
			this.targetButton.StartBringIntoView(new BringIntoViewOptions());
		}

		public void OnUseTestWindowBoundsCheckBoxChecked(object sender, RoutedEventArgs args)
		{
			var tip = getTeachingTip();
			Rect windowRect = new Rect(double.Parse(this.TestWindowBoundsXTextBox.Text),
									   double.Parse(this.TestWindowBoundsYTextBox.Text),
									   double.Parse(this.TestWindowBoundsWidthTextBox.Text),
									   double.Parse(this.TestWindowBoundsHeightTextBox.Text));
#if HAS_UNO
			TeachingTipTestHooks.SetUseTestWindowBounds(tip, true);
			TeachingTipTestHooks.SetTestWindowBounds(tip, windowRect);
#endif
			if (testWindowBounds == null)
			{
				testWindowBounds = new Popup();
				testWindowBounds.IsHitTestVisible = false;
			}
			Grid windowBounds = new Grid();
			windowBounds.Width = windowRect.Width;
			windowBounds.Height = windowRect.Height;
			windowBounds.Background = new SolidColorBrush(Colors.Transparent);
			windowBounds.BorderBrush = new SolidColorBrush(Colors.Red);
			windowBounds.BorderThickness = new Thickness(1.0);
			testWindowBounds.Child = windowBounds;
			testWindowBounds.HorizontalOffset = windowRect.X;
			testWindowBounds.VerticalOffset = windowRect.Y;
			testWindowBounds.IsOpen = true;
		}

		public void OnUseTestSreenBoundsCheckBoxChecked(object sender, RoutedEventArgs args)
		{
			var tip = getTeachingTip();
			Rect screenRect = new Rect(double.Parse(this.TestScreenBoundsXTextBox.Text),
									   double.Parse(this.TestScreenBoundsYTextBox.Text),
									   double.Parse(this.TestScreenBoundsWidthTextBox.Text),
									   double.Parse(this.TestScreenBoundsHeightTextBox.Text));
#if HAS_UNO
			TeachingTipTestHooks.SetUseTestScreenBounds(tip, true);
			TeachingTipTestHooks.SetTestScreenBounds(tip, screenRect);
#endif
			if (testScreenBounds == null)
			{
				testScreenBounds = new Popup();
				testScreenBounds.IsHitTestVisible = false;
			}
			Grid windowBounds = new Grid();
			windowBounds.Width = screenRect.Width;
			windowBounds.Height = screenRect.Height;
			windowBounds.Background = new SolidColorBrush(Colors.Transparent);
			windowBounds.BorderBrush = new SolidColorBrush(Colors.Blue);
			windowBounds.BorderThickness = new Thickness(1.0);
			testScreenBounds.Child = windowBounds;
			testScreenBounds.HorizontalOffset = screenRect.X;
			testScreenBounds.VerticalOffset = screenRect.Y;
			testScreenBounds.IsOpen = true;
		}

		public void OnUseTestWindowBoundsCheckBoxUnchecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetUseTestWindowBounds(getTeachingTip(), false);
			testWindowBounds.IsOpen = false;
#endif
		}

		public void OnUseTestScreenBoundsCheckBoxUnchecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetUseTestScreenBounds(getTeachingTip(), false);
			testScreenBounds.IsOpen = false;
#endif
		}

		public void OnTipFollowsTargetCheckBoxChecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetTipFollowsTarget(getTeachingTip(), true);
#endif
		}

		public void OnTipFollowsTargetCheckBoxUnchecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetTipFollowsTarget(getTeachingTip(), false);
#endif
		}

		public void OnReturnTopForOutOfWindowPlacementCheckBoxChecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetReturnTopForOutOfWindowPlacement(getTeachingTip(), true);
#endif
		}

		public void OnReturnTopForOutOfWindowPlacementCheckBoxUnchecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetReturnTopForOutOfWindowPlacement(getTeachingTip(), false);
#endif
		}

		public void OnSetPreferredPlacementButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.PreferredPlacementComboBox.SelectedItem == PlacementTop)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Top;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementBottom)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Bottom;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementLeft)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Left;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementRight)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Right;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementTopRight)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.TopRight;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementTopLeft)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.TopLeft;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementBottomRight)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.BottomRight;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementBottomLeft)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.BottomLeft;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementLeftTop)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.LeftTop;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementLeftBottom)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.LeftBottom;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementRightTop)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.RightTop;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementRightBottom)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.RightBottom;
			}
			else if (this.PreferredPlacementComboBox.SelectedItem == PlacementCenter)
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Center;
			}
			else
			{
				getTeachingTip().PreferredPlacement = TeachingTipPlacementMode.Auto;
			}
		}

		public void OnSetIsLightDismissEnabledButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.IsLightDismissEnabledComboBox.SelectedItem == IsLightDismissFalse)
			{
				getTeachingTip().IsLightDismissEnabled = false;
			}
			else
			{
				getTeachingTip().IsLightDismissEnabled = true;
			}
		}

		public void OnSetShouldConstrainToRootBoundsButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.ShouldConstrainToRootBoundsComboBox.SelectedItem == ShouldConstrainToRootBoundsFalse)
			{
				getTeachingTip().ShouldConstrainToRootBounds = false;
			}
			else
			{
				getTeachingTip().ShouldConstrainToRootBounds = true;
			}
		}

		public void OnSetPlacementMarginButtonClicked(object sender, RoutedEventArgs args)
		{
			getTeachingTip().PlacementMargin = new Thickness(Double.Parse(this.PlacementMarginTextBox.Text));
		}

		public void OnSetTailVisibilityButtonClicked(object sender, RoutedEventArgs args)
		{
			if (this.TailVisibilityComboBox.SelectedItem == TailVisibilityAuto)
			{
				getTeachingTip().TailVisibility = TeachingTipTailVisibility.Auto;
			}
			else if (this.TailVisibilityComboBox.SelectedItem == TailVisibilityVisible)
			{
				getTeachingTip().TailVisibility = TeachingTipTailVisibility.Visible;
			}
			else
			{
				getTeachingTip().TailVisibility = TeachingTipTailVisibility.Collapsed;
			}
		}

		public void OnSetTargetButtonClicked(object sender, RoutedEventArgs args)
		{
			getTeachingTip().Target = this.targetButton;
		}

		public void OnUntargetButtonClicked(object sender, RoutedEventArgs args)
		{
			getTeachingTip().Target = null;
		}

		public void OnShowButtonClicked(object sender, RoutedEventArgs args)
		{
			getTeachingTip().IsOpen = true;
			switch (tipLocation)
			{
				case TipLocation.VisualTree:
					if (TeachingTipInVisualTreeRoot == null)
					{
						getCancelClosesInTeachingTip().Loaded += TeachingTipInVisualTreeRoot_Loaded; ;
					}
					else
					{
						TeachingTipInVisualTreeRoot.SizeChanged += TeachingTip_SizeChanged;
						TeachingTip_SizeChanged(TeachingTipInVisualTreeRoot, null);
					}
					break;
				default:
					if (TeachingTipInResourcesRoot == null)
					{
						getCancelClosesInTeachingTip().Loaded += TeachingTipInResourcesRoot_Loaded;
					}
					else
					{
						TeachingTipInResourcesRoot.SizeChanged += TeachingTip_SizeChanged;
						TeachingTip_SizeChanged(TeachingTipInResourcesRoot, null);
					}
					break;
			}

			CurrentCancelClosesCheckBox = getCancelClosesInTeachingTip();
			NotifyPropertyChanged("CurrentCancelClosesCheckBox");
		}

		public void OnShowButtonClickedRightEdge(object sender, RoutedEventArgs args)
		{
			TeachingTipInResourcesOnEdge.IsOpen = true;
			TeachingTipInResourcesOnEdge.SizeChanged += TeachingTip_SizeChanged;
			TeachingTip_SizeChanged(TeachingTipInResourcesOnEdge, null);
		}

		public void GetEdgeTeachingTipOffset_Clicked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			EdgeTeachingTipOffset.Text = TeachingTipTestHooks.GetHorizontalOffset(TeachingTipInResourcesOnEdge).ToString()
				+ ";" + TeachingTipTestHooks.GetVerticalOffset(TeachingTipInResourcesOnEdge).ToString();
#endif
		}

		public void OnShowAfterDelayButtonClicked(object sender, RoutedEventArgs args)
		{
			showTimer = new DispatcherTimer();
			showTimer.Interval = new TimeSpan(0, 0, 2);
			showTimer.Tick += ShowTimerTick;
			showTimer.Start();
		}

		private void ShowTimerTick(object sender, object e)
		{
			showTimer.Tick -= ShowTimerTick;
			showTimer.Stop();
			OnShowButtonClicked(null, null);
		}

		private void TeachingTipInResourcesRoot_Loaded(object sender, RoutedEventArgs e)
		{
			((FrameworkElement)sender).Loaded -= TeachingTipInResourcesRoot_Loaded;
			TeachingTipInResourcesRoot = getTeachingTipRoot(getCancelClosesInTeachingTip());
			TeachingTipInResourcesRoot.SizeChanged += TeachingTip_SizeChanged;
			TeachingTip_SizeChanged(TeachingTipInResourcesRoot, null);
#if HAS_UNO
			this.TitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetTitleVisibility(this.TeachingTipInResources).ToString();
			this.SubtitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetSubtitleVisibility(this.TeachingTipInResources).ToString();
#endif
		}

		private void TeachingTipInVisualTreeRoot_Loaded(object sender, RoutedEventArgs e)
		{
			((FrameworkElement)sender).Loaded -= TeachingTipInVisualTreeRoot_Loaded;
			TeachingTipInVisualTreeRoot = getTeachingTipRoot(getCancelClosesInTeachingTip());
			TeachingTipInVisualTreeRoot.SizeChanged += TeachingTip_SizeChanged;
			TeachingTip_SizeChanged(TeachingTipInVisualTreeRoot, null);

#if HAS_UNO
			this.TitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetTitleVisibility(this.TeachingTipInVisualTree).ToString();
			this.SubtitleVisibilityTextBlock.Text = TeachingTipTestHooks.GetSubtitleVisibility(this.TeachingTipInVisualTree).ToString();
#endif
		}

		public void OnCloseButtonClicked(object sender, RoutedEventArgs args)
		{
			getTeachingTip().IsOpen = false;
		}

		public void OnSetAutomationNameButtonClicked(object sender, RoutedEventArgs args)
		{
			var tip = getTeachingTip();
			if (this.AutomationNameComboBox.SelectedItem == AutomationNameVisualTree)
			{
				AutomationProperties.SetName(tip, "TeachingTipInVisualTree");
			}
			else if (this.AutomationNameComboBox.SelectedItem == AutomationNameResources)
			{
				AutomationProperties.SetName(tip, "TeachingTipInResources");
			}
			else
			{
				AutomationProperties.SetName(tip, "");
			}
		}

		public void OnSetTargetVerticalAlignmentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (TargetVerticalAlignmentComboBox.SelectedItem == TargetVerticalAlignmentTop)
			{
				getTeachingTip().VerticalAlignment = VerticalAlignment.Top;
			}
			else if (TargetVerticalAlignmentComboBox.SelectedItem == TargetVerticalAlignmentCenter)
			{
				getTeachingTip().VerticalAlignment = VerticalAlignment.Center;
			}
			else
			{
				getTeachingTip().VerticalAlignment = VerticalAlignment.Bottom;
			}
			OnGetTargetBoundsButtonClicked(null, null);
		}
		public void OnSetTargetHorizontalAlignmentButtonClicked(object sender, RoutedEventArgs args)
		{
			if (TargetHorizontalAlignmentComboBox.SelectedItem == TargetHorizontalAlignmentLeft)
			{
				this.targetButton.HorizontalAlignment = HorizontalAlignment.Left;
			}
			else if (TargetHorizontalAlignmentComboBox.SelectedItem == TargetHorizontalAlignmentCenter)
			{
				this.targetButton.HorizontalAlignment = HorizontalAlignment.Center;
			}
			else
			{
				this.targetButton.HorizontalAlignment = HorizontalAlignment.Right;
			}
			OnGetTargetBoundsButtonClicked(null, null);
		}

		public void OnSetAnimationParametersButtonClicked(object sender, RoutedEventArgs args)
		{
			var expandEasing = global::Windows.UI.Xaml.Window.Current.Compositor.CreateCubicBezierEasingFunction(
				new System.Numerics.Vector2(float.Parse(this.ExpandControlPoint1X.Text), float.Parse(this.ExpandControlPoint1Y.Text)),
				new System.Numerics.Vector2(float.Parse(this.ExpandControlPoint2X.Text), float.Parse(this.ExpandControlPoint2Y.Text)));

			var contractEasing = global::Windows.UI.Xaml.Window.Current.Compositor.CreateCubicBezierEasingFunction(
				new System.Numerics.Vector2(float.Parse(this.ExpandControlPoint1X.Text), float.Parse(this.ExpandControlPoint1Y.Text)),
				new System.Numerics.Vector2(float.Parse(this.ExpandControlPoint2X.Text), float.Parse(this.ExpandControlPoint2Y.Text)));

			var tip = getTeachingTip();
#if HAS_UNO
			TeachingTipTestHooks.SetExpandEasingFunction(tip, expandEasing);
			TeachingTipTestHooks.SetContractEasingFunction(tip, contractEasing);
#endif
		}

		public void OnSetAnimationDurationsButtonClicked(object sender, RoutedEventArgs args)
		{
			var expandDuration = new TimeSpan(0, 0, 0, 0, int.Parse(ExpandAnimationDuration.Text));
			var contractDuration = new TimeSpan(0, 0, 0, 0, int.Parse(ContractAnimationDuration.Text));
#if HAS_UNO
			TeachingTipTestHooks.SetExpandAnimationDuration(getTeachingTip(), expandDuration);
			TeachingTipTestHooks.SetContractAnimationDuration(getTeachingTip(), contractDuration);
#endif
		}

		public void ContentElevationSliderChanged(object sender, RangeBaseValueChangedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetContentElevation(getTeachingTip(), (float)args.NewValue);
#endif
		}

		public void TailElevationSliderChanged(object sender, RangeBaseValueChangedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetTailElevation(getTeachingTip(), (float)args.NewValue);
#endif
		}

		public void OnTipShadowChecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetTipShouldHaveShadow(getTeachingTip(), true);
#endif
		}

		public void OnTipShadowUnchecked(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			TeachingTipTestHooks.SetTipShouldHaveShadow(getTeachingTip(), false);
#endif
		}

		public void OnTeachingTipClosed(object sender, TeachingTipClosedEventArgs args)
		{
			if (lstTeachingTipEvents != null)
			{
				lstTeachingTipEvents.Items.Add(lstTeachingTipEvents.Items.Count.ToString() + ") " + args.ToString() + " Reason: " + args.Reason.ToString());
				lstTeachingTipEvents.ScrollIntoView(lstTeachingTipEvents.Items.Last<object>());
			}
		}

		public void OnTeachingTipClosing(TeachingTip sender, TeachingTipClosingEventArgs args)
		{
			if (lstTeachingTipEvents != null)
			{
				lstTeachingTipEvents.Items.Add(lstTeachingTipEvents.Items.Count.ToString() + ") " + args.ToString() + " Reason: " + args.Reason.ToString());
				lstTeachingTipEvents.ScrollIntoView(lstTeachingTipEvents.Items.Last<object>());
			}

			CheckBox cancelClosesCheckBox = null;
			if (sender == TeachingTipInResources)
			{
				cancelClosesCheckBox = CancelClosesCheckBoxInResources;
			}
			else
			{
				cancelClosesCheckBox = CancelClosesCheckBoxInVisualTree;
			}

			if (cancelClosesCheckBox != null && cancelClosesCheckBox.IsChecked == true)
			{
				deferral = args.GetDeferral();
				args.Cancel = true;
				timer = new DispatcherTimer();
				timer.Tick += Timer_Tick;
				timer.Interval = new TimeSpan(0, 0, 1);
				timer.Start();
			}
		}

		private void Timer_Tick(object sender, object e)
		{
			timer.Stop();
			deferral.Complete();
		}

		public void OnTeachingTipActionButtonClicked(object sender, object args)
		{
			lstTeachingTipEvents.Items.Add(lstTeachingTipEvents.Items.Count.ToString() + ") " + "Action Button Clicked Event");
			lstTeachingTipEvents.ScrollIntoView(lstTeachingTipEvents.Items.Last<object>());
		}

		public void OnTeachingTipCloseButtonClicked(object sender, object args)
		{
			lstTeachingTipEvents.Items.Add(lstTeachingTipEvents.Items.Count.ToString() + ") " + "Close Button Clicked Event");
			lstTeachingTipEvents.ScrollIntoView(lstTeachingTipEvents.Items.Last<object>());
		}

		private void BtnClearTeachingTipEvents_Click(object sender, RoutedEventArgs e)
		{
			this.lstTeachingTipEvents.Items.Clear();
		}

		private TeachingTip getTeachingTip()
		{
			switch (tipLocation)
			{
				case TipLocation.VisualTree:
					return this.TeachingTipInVisualTree;
				default:
					return this.TeachingTipInResources;
			}
		}

		private CheckBox getCancelClosesInTeachingTip()
		{
			switch (tipLocation)
			{
				case TipLocation.VisualTree:
					return this.CancelClosesCheckBoxInVisualTree;
				default:
					return this.CancelClosesCheckBoxInResources;
			}
		}

		private FrameworkElement getTeachingTipRoot(UIElement cancelClosesCheckBox)
		{
			DependencyObject current = cancelClosesCheckBox;
			for (int i = 0; i < 11; i++)
			{
				current = VisualTreeHelper.GetParent(current);
			}
			return (FrameworkElement)current;
		}

		public bool IsPageRTL
		{
			get => FlowDirection == FlowDirection.RightToLeft;
			set
			{
				FlowDirection newFlowDirection = value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
				if (FlowDirection != newFlowDirection)
				{
					FlowDirection = newFlowDirection;
					NotifyPropertyChanged();
				}
			}
		}

		private void OnPageThemeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedItem = ((ComboBoxItem)PageThemeComboBox.SelectedItem);
			if (String.Equals(selectedItem?.Content, "Light"))
			{
				RequestedTheme = ElementTheme.Light;
			}
			else if (String.Equals(selectedItem?.Content, "Dark"))
			{
				RequestedTheme = ElementTheme.Dark;
			}
			else
			{
				RequestedTheme = ElementTheme.Default;
			}
		}

		public Button ActionButton
		{
			get
			{
#if HAS_UNO
				var popupChild = TeachingTipTestHooks.GetPopup(getTeachingTip())?.Child as FrameworkElement;
				if (popupChild != null)
				{
					return (Button)FindVisualChildByName(popupChild, "ActionButton");
				}
#endif
				return null;
			}
		}

		public string BrushToString(Brush brush)
		{
			if (brush is SolidColorBrush solidBrush)
			{
				return String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", solidBrush.Color.A, solidBrush.Color.R, solidBrush.Color.G, solidBrush.Color.B);
			}

			return "Unknown";
		}

		private void RemoveTeachingTipButton_Click(object sender, RoutedEventArgs e)
		{
			ContentStackPanel.Children.Remove(TeachingTipInVisualTree);
			ContentStackPanel.Children.Remove(TeachingTipInResources);
		}

		private void RemoveTeachingTipTextBlockContent_Unloaded(object sender, RoutedEventArgs e)
		{
			VisualTreeTeachingTipContentTextBlockUnloaded.IsChecked = true;
		}

		private void RemoveOpenButton_Click(object sender, RoutedEventArgs e)
		{
			ContentStackPanel.Children.Remove(targetButton);
		}

	}
}
