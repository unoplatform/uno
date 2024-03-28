// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Dispatching;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewDynamicPage : TestPage
{
	private Object asyncEventReportingLock = new Object();
	private List<string> lstAsyncEventMessage = new List<string>();
	private Image largeImg;
	private Image wuxLargeImg;
	private Rectangle rectangle = null;
	private Rectangle wuxRectangle = null;
	private Button button = null;
	private Button wuxButton = null;
	private Border border = null;
	private Border wuxBorder = null;
	private StackPanel sp1 = null;
	private StackPanel wuxSp1 = null;
	private StackPanel sp2 = null;
	private StackPanel wuxSp2 = null;
	private Viewbox viewbox = null;
	private Viewbox wuxViewbox = null;
	private ScrollView scrollView = null;
	private PointerEventHandler contentPointerWheelChangedEventHandler = null;
	private PointerEventHandler scrollPresenterPointerWheelChangedEventHandler = null;
	private PointerEventHandler scrollViewPointerWheelChangedEventHandler = null;

	public ScrollViewDynamicPage()
	{
		this.InitializeComponent();
		this.Loaded += ScrollViewDynamicPage_Loaded;
		CreateChildren();
	}

	~ScrollViewDynamicPage()
	{
	}

	//protected override void OnNavigatedFrom(NavigationEventArgs e)
	//{
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

	//    MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

	//    base.OnNavigatedFrom(e);
	//}

	private void ScrollViewDynamicPage_Loaded(object sender, RoutedEventArgs args)
	{
		UseScrollView(this.markupScrollView);
	}

	private void ChkScrollViewProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (grdScrollViewProperties != null)
			grdScrollViewProperties.Visibility = Visibility.Visible;
	}

	private void ChkScrollViewProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdScrollViewProperties != null)
			grdScrollViewProperties.Visibility = Visibility.Collapsed;
	}

	private void ChkScrollPresenterClonedProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (grdScrollPresenterClonedProperties != null)
			grdScrollPresenterClonedProperties.Visibility = Visibility.Visible;
	}

	private void ChkScrollPresenterClonedProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdScrollPresenterClonedProperties != null)
			grdScrollPresenterClonedProperties.Visibility = Visibility.Collapsed;
	}

	private void ChkContentProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (grdContentProperties != null)
			grdContentProperties.Visibility = Visibility.Visible;
	}

	private void ChkContentProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdContentProperties != null)
			grdContentProperties.Visibility = Visibility.Collapsed;
	}

	private void ChkLogs_Checked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Visible;
	}

	private void ChkLogs_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Collapsed;
	}

	private void BtnGetContentOrientation_Click(object sender, RoutedEventArgs e)
	{
		UpdateContentOrientation();
	}

	private void BtnGetHorizontalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollMode();
	}

	private void BtnGetHorizontalScrollChainMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollChainMode();
	}

	private void BtnGetHorizontalScrollRailMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollRailMode();
	}

	private void BtnGetVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollMode();
	}

	private void BtnGetVerticalScrollChainMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollChainMode();
	}

	private void BtnGetVerticalScrollRailMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollRailMode();
	}

	private void BtnGetZoomMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomMode();
	}

	private void BtnGetZoomChainMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomChainMode();
	}

	private void BtnGetIgnoredInputKinds_Click(object sender, RoutedEventArgs e)
	{
		UpdateIgnoredInputKinds();
	}

	private void BtnGetMinZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		UpdateMinZoomFactor();
	}

	private void BtnGetMaxZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		UpdateMaxZoomFactor();
	}

	private void BtnSetContentOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingContentOrientation co = (ScrollingContentOrientation)cmbContentOrientation.SelectedIndex;
			scrollView.ContentOrientation = co;

			switch (co)
			{
				case ScrollingContentOrientation.Horizontal:
					wuxScrollViewer.HorizontalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.HorizontalScrollBarVisibility);
					wuxScrollViewer.VerticalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					break;
				case ScrollingContentOrientation.Vertical:
					wuxScrollViewer.HorizontalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					wuxScrollViewer.VerticalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.VerticalScrollBarVisibility);
					break;
				case ScrollingContentOrientation.Both:
					wuxScrollViewer.HorizontalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.HorizontalScrollBarVisibility);
					wuxScrollViewer.VerticalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.VerticalScrollBarVisibility);
					break;
				case ScrollingContentOrientation.None:
					wuxScrollViewer.HorizontalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					wuxScrollViewer.VerticalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					break;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetHorizontalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingScrollMode ssm = (ScrollingScrollMode)cmbHorizontalScrollMode.SelectedIndex;
			scrollView.HorizontalScrollMode = ssm;

			wuxScrollViewer.HorizontalScrollMode = MuxScrollModeToWuxScrollMode(ssm);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetHorizontalScrollChainMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingChainMode scm = (ScrollingChainMode)cmbHorizontalScrollChainMode.SelectedIndex;
			scrollView.HorizontalScrollChainMode = scm;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetHorizontalScrollRailMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingRailMode srm = (ScrollingRailMode)cmbHorizontalScrollRailMode.SelectedIndex;
			scrollView.HorizontalScrollRailMode = srm;

			wuxScrollViewer.IsHorizontalRailEnabled = MuxRailModeToWuxRailMode(srm);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingScrollMode ssm = (ScrollingScrollMode)cmbVerticalScrollMode.SelectedIndex;
			scrollView.VerticalScrollMode = ssm;

			wuxScrollViewer.VerticalScrollMode = MuxScrollModeToWuxScrollMode(ssm);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetVerticalScrollChainMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingChainMode scm = (ScrollingChainMode)cmbVerticalScrollChainMode.SelectedIndex;
			scrollView.VerticalScrollChainMode = scm;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetVerticalScrollRailMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingRailMode srm = (ScrollingRailMode)cmbVerticalScrollRailMode.SelectedIndex;
			scrollView.VerticalScrollRailMode = srm;

			wuxScrollViewer.IsVerticalRailEnabled = MuxRailModeToWuxRailMode(srm);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetZoomMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingZoomMode szm = (ScrollingZoomMode)cmbZoomMode.SelectedIndex;
			scrollView.ZoomMode = szm;

			wuxScrollViewer.ZoomMode = MuxZoomModeToWuxZoomMode(szm);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetZoomChainMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingChainMode scm = (ScrollingChainMode)cmbZoomChainMode.SelectedIndex;
			scrollView.ZoomChainMode = scm;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetIgnoredInputKinds_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingInputKinds ignoredInputKinds;

			switch (cmbIgnoredInputKinds.SelectedIndex)
			{
				case 0:
					ignoredInputKinds = ScrollingInputKinds.None;
					break;
				case 1:
					ignoredInputKinds = ScrollingInputKinds.Touch;
					break;
				case 2:
					ignoredInputKinds = ScrollingInputKinds.Pen;
					break;
				case 3:
					ignoredInputKinds = ScrollingInputKinds.MouseWheel;
					break;
				case 4:
					ignoredInputKinds = ScrollingInputKinds.Keyboard;
					break;
				case 5:
					ignoredInputKinds = ScrollingInputKinds.Gamepad;
					break;
				default:
					ignoredInputKinds = ScrollingInputKinds.All;
					break;
			}

			scrollView.IgnoredInputKinds = ignoredInputKinds;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetMinZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.MinZoomFactor = Convert.ToDouble(txtMinZoomFactor.Text);

			wuxScrollViewer.MinZoomFactor = (float)scrollView.MinZoomFactor;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetMaxZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.MaxZoomFactor = Convert.ToDouble(txtMaxZoomFactor.Text);

			wuxScrollViewer.MaxZoomFactor = (float)scrollView.MaxZoomFactor;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetHorizontalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalAnchorRatio();
	}

	private void BtnSetHorizontalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.HorizontalAnchorRatio = Convert.ToDouble(txtHorizontalAnchorRatio.Text);

			wuxScrollViewer.HorizontalAnchorRatio = scrollView.HorizontalAnchorRatio;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetVerticalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalAnchorRatio();
	}

	private void BtnSetVerticalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.VerticalAnchorRatio = Convert.ToDouble(txtVerticalAnchorRatio.Text);

			wuxScrollViewer.VerticalAnchorRatio = scrollView.VerticalAnchorRatio;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetHorizontalOffset_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalOffset();
	}

	private void BtnGetVerticalOffset_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalOffset();
	}

	private void BtnGetZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomFactor();
	}

	private void BtnGetExtentWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateExtentWidth();
	}

	private void BtnGetExtentHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateExtentHeight();
	}

	private void BtnGetComputedHorizontalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateTblComputedHorizontalScrollMode();
	}

	private void BtnGetComputedVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateTblComputedVerticalScrollMode();
	}

	private void UpdateTblComputedHorizontalScrollMode()
	{
		try
		{
			tblComputedHorizontalScrollMode.Text = scrollView.ComputedHorizontalScrollMode.ToString();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateTblComputedVerticalScrollMode()
	{
		try
		{
			tblComputedVerticalScrollMode.Text = scrollView.ComputedVerticalScrollMode.ToString();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbHorizontalScrollBarVisibility()
	{
		try
		{
			cmbHorizontalScrollBarVisibility.SelectedIndex = (int)scrollView.HorizontalScrollBarVisibility;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbVerticalScrollBarVisibility()
	{
		try
		{
			cmbVerticalScrollBarVisibility.SelectedIndex = (int)scrollView.VerticalScrollBarVisibility;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbXYFocusKeyboardNavigation()
	{
		try
		{
			cmbXYFocusKeyboardNavigation.SelectedIndex = (int)scrollView.XYFocusKeyboardNavigation;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateHorizontalAnchorRatio()
	{
		txtHorizontalAnchorRatio.Text = scrollView.HorizontalAnchorRatio.ToString();
	}

	private void UpdateVerticalAnchorRatio()
	{
		txtVerticalAnchorRatio.Text = scrollView.VerticalAnchorRatio.ToString();
	}

	private void UpdateHorizontalOffset()
	{
		txtHorizontalOffset.Text = scrollView.HorizontalOffset.ToString();
	}

	private void UpdateVerticalOffset()
	{
		txtVerticalOffset.Text = scrollView.VerticalOffset.ToString();
	}

	private void UpdateZoomFactor()
	{
		txtZoomFactor.Text = scrollView.ZoomFactor.ToString();
	}

	private void UpdateExtentWidth()
	{
		txtExtentWidth.Text = scrollView.ExtentWidth.ToString();

		if (Math.Abs(scrollView.ExtentWidth - wuxScrollViewer.ExtentWidth / wuxScrollViewer.ZoomFactor) > 0.0001)
		{
			lstLogs.Items.Add($"muxScrollView.ExtentWidth={scrollView.ExtentWidth} != wuxScrollViewer.ExtentWidth/wuxScrollViewer.ZoomFactor={wuxScrollViewer.ExtentWidth / wuxScrollViewer.ZoomFactor}");
		}
	}

	private void UpdateExtentHeight()
	{
		txtExtentHeight.Text = scrollView.ExtentHeight.ToString();

		if (Math.Abs(scrollView.ExtentHeight - wuxScrollViewer.ExtentHeight / wuxScrollViewer.ZoomFactor) > 0.0001)
		{
			lstLogs.Items.Add($"muxScrollView.ExtentHeight={scrollView.ExtentHeight} != wuxScrollViewer.ExtentHeight/wuxScrollViewer.ZoomFactor={wuxScrollViewer.ExtentHeight / wuxScrollViewer.ZoomFactor}");
		}
	}

	private void BtnGetWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateWidth();
	}

	private void BtnSetWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.Width = Convert.ToDouble(txtWidth.Text);

			wuxScrollViewer.Width = scrollView.Width;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateHeight();
	}

	private void BtnSetHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.Height = Convert.ToDouble(txtHeight.Text);

			wuxScrollViewer.Height = scrollView.Height;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		switch (cmbBackground.SelectedIndex)
		{
			case 0:
				scrollView.Background = null;
				break;
			case 1:
				scrollView.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
				break;
			case 2:
				scrollView.Background = new SolidColorBrush(Windows.UI.Colors.AliceBlue);
				break;
			case 3:
				scrollView.Background = new SolidColorBrush(Windows.UI.Colors.Aqua);
				break;
		}

		wuxScrollViewer.Background = scrollView.Background;
	}

	private void CmbContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			FrameworkElement currentContent = scrollView.Content as FrameworkElement;

			if (currentContent != null && contentPointerWheelChangedEventHandler != null)
			{
				currentContent.RemoveHandler(UIElement.PointerWheelChangedEvent, contentPointerWheelChangedEventHandler);
			}

			FrameworkElement newContent = null;
			FrameworkElement wuxNewContent = null;

			switch (cmbContent.SelectedIndex)
			{
				case 1:
					newContent = smallImg;
					wuxNewContent = wuxSmallImg;
					break;
				case 2:
					newContent = largeImg;
					wuxNewContent = wuxLargeImg;
					break;
				case 3:
					newContent = rectangle;
					wuxNewContent = wuxRectangle;
					break;
				case 4:
					newContent = button;
					wuxNewContent = wuxButton;
					break;
				case 5:
					newContent = border;
					wuxNewContent = wuxBorder;
					break;
				case 6:
					newContent = sp1;
					wuxNewContent = wuxSp1;
					break;
				case 7:
					newContent = sp2;
					wuxNewContent = wuxSp2;
					break;
				case 8:
					newContent = viewbox;
					wuxNewContent = wuxViewbox;
					break;
				case 9:
					newContent = spH1;
					wuxNewContent = wuxSpH1;
					break;
			}

			if (chkPreserveProperties.IsChecked == true && currentContent != null && newContent != null && wuxNewContent != null)
			{
				newContent.MinWidth = currentContent.MinWidth;
				newContent.Width = currentContent.Width;
				newContent.MaxWidth = currentContent.MaxWidth;
				newContent.MinHeight = currentContent.MinHeight;
				newContent.Height = currentContent.Height;
				newContent.MaxHeight = currentContent.MaxHeight;
				newContent.Margin = currentContent.Margin;
				newContent.HorizontalAlignment = currentContent.HorizontalAlignment;
				newContent.VerticalAlignment = currentContent.VerticalAlignment;

				if (currentContent is Control && newContent is Control)
				{
					((Control)newContent).Padding = ((Control)currentContent).Padding;
				}

				wuxNewContent.MinWidth = currentContent.MinWidth;
				wuxNewContent.Width = currentContent.Width;
				wuxNewContent.MaxWidth = currentContent.MaxWidth;
				wuxNewContent.MinHeight = currentContent.MinHeight;
				wuxNewContent.Height = currentContent.Height;
				wuxNewContent.MaxHeight = currentContent.MaxHeight;
				wuxNewContent.Margin = currentContent.Margin;
				wuxNewContent.HorizontalAlignment = currentContent.HorizontalAlignment;
				wuxNewContent.VerticalAlignment = currentContent.VerticalAlignment;

				if (currentContent is Control && wuxNewContent is Control)
				{
					((Control)wuxNewContent).Padding = ((Control)currentContent).Padding;
				}
			}

			scrollView.Content = newContent;

			if (newContent != null && chkLogScrollPresenterEvents.IsChecked == true)
			{
				if (contentPointerWheelChangedEventHandler == null)
				{
					contentPointerWheelChangedEventHandler = new PointerEventHandler(Content_PointerWheelChanged);
				}

				newContent.AddHandler(UIElement.PointerWheelChangedEvent, contentPointerWheelChangedEventHandler, true);
			}

			wuxScrollViewer.Content = wuxNewContent;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());

			UpdateContent();
		}
	}

	private void BtnGetCurrentAnchor_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (scrollView.CurrentAnchor == null)
			{
				txtCurrentAnchor.Text = "null";
			}
			else
			{
				FrameworkElement currentAnchorAsFE = scrollView.CurrentAnchor as FrameworkElement;

				txtCurrentAnchor.Text = currentAnchorAsFE == null ? "UIElement" : currentAnchorAsFE.Name;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbHorizontalScrollBarVisibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			scrollView.HorizontalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbHorizontalScrollBarVisibility.SelectedIndex;

			ScrollingContentOrientation co = (ScrollingContentOrientation)cmbContentOrientation.SelectedIndex;
			switch (co)
			{
				case ScrollingContentOrientation.Vertical:
				case ScrollingContentOrientation.None:
					wuxScrollViewer.HorizontalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					break;
				default:
					wuxScrollViewer.HorizontalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.HorizontalScrollBarVisibility);
					break;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbVerticalScrollBarVisibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			scrollView.VerticalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbVerticalScrollBarVisibility.SelectedIndex;

			ScrollingContentOrientation co = (ScrollingContentOrientation)cmbContentOrientation.SelectedIndex;
			switch (co)
			{
				case ScrollingContentOrientation.Horizontal:
				case ScrollingContentOrientation.None:
					wuxScrollViewer.VerticalScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
					break;
				default:
					wuxScrollViewer.VerticalScrollBarVisibility = MuxScrollBarVisibilityToWuxScrollBarVisibility(scrollView.VerticalScrollBarVisibility);
					break;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbXYFocusKeyboardNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			scrollView.XYFocusKeyboardNavigation = (XYFocusKeyboardNavigationMode)cmbXYFocusKeyboardNavigation.SelectedIndex;

			wuxScrollViewer.XYFocusKeyboardNavigation = scrollView.XYFocusKeyboardNavigation;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetMargin_Click(object sender, RoutedEventArgs e)
	{
		UpdateMargin();
	}

	private void BtnSetMargin_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.Margin = GetThicknessFromString(txtMargin.Text);

			wuxScrollViewer.Margin = scrollView.Margin;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetPadding_Click(object sender, RoutedEventArgs e)
	{
		UpdatePadding();
	}

	private void BtnSetPadding_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.Padding = GetThicknessFromString(txtPadding.Text);

			wuxScrollViewer.Padding = scrollView.Padding;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void ChkIsEnabled_Checked(object sender, RoutedEventArgs e)
	{
		scrollView.IsEnabled = true;

		wuxScrollViewer.IsEnabled = true;
	}

	private void ChkIsEnabled_Unchecked(object sender, RoutedEventArgs e)
	{
		scrollView.IsEnabled = false;

		wuxScrollViewer.IsEnabled = false;
	}

	private void ChkIsTabStop_Checked(object sender, RoutedEventArgs e)
	{
		scrollView.IsTabStop = true;

		wuxScrollViewer.IsTabStop = true;
	}

	private void ChkIsTabStop_Unchecked(object sender, RoutedEventArgs e)
	{
		scrollView.IsTabStop = false;

		wuxScrollViewer.IsTabStop = false;
	}

	private void BtnGetContentMinWidth_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentMinWidth.Text = string.Empty;
		else
			txtContentMinWidth.Text = ((FrameworkElement)scrollView.Content).MinWidth.ToString();
	}

	private void BtnGetContentWidth_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentWidth.Text = string.Empty;
		else
			txtContentWidth.Text = ((FrameworkElement)scrollView.Content).Width.ToString();
	}

	private void BtnGetContentMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentMaxWidth.Text = string.Empty;
		else
			txtContentMaxWidth.Text = ((FrameworkElement)scrollView.Content).MaxWidth.ToString();
	}

	private void BtnSetContentMinWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double minWidth = Convert.ToDouble(txtContentMinWidth.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).MinWidth = minWidth;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).MinWidth = minWidth;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetContentWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double width = Convert.ToDouble(txtContentWidth.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).Width = width;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).Width = width;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetContentMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double maxWidth = Convert.ToDouble(txtContentMaxWidth.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).MaxWidth = maxWidth;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).MaxWidth = maxWidth;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentMinHeight_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentMinHeight.Text = string.Empty;
		else
			txtContentMinHeight.Text = ((FrameworkElement)scrollView.Content).MinHeight.ToString();
	}

	private void BtnGetContentHeight_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentHeight.Text = string.Empty;
		else
			txtContentHeight.Text = ((FrameworkElement)scrollView.Content).Height.ToString();
	}

	private void BtnGetContentMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentMaxHeight.Text = string.Empty;
		else
			txtContentMaxHeight.Text = ((FrameworkElement)scrollView.Content).MaxHeight.ToString();
	}

	private void BtnSetContentMinHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double minHeight = Convert.ToDouble(txtContentMinHeight.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).MinHeight = minHeight;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).MinHeight = minHeight;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetContentHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double height = Convert.ToDouble(txtContentHeight.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).Height = height;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).Height = height;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnSetContentMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double maxHeight = Convert.ToDouble(txtContentMaxHeight.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).MaxHeight = maxHeight;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).MaxHeight = maxHeight;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentMargin_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentMargin.Text = string.Empty;
		else
			txtContentMargin.Text = ((FrameworkElement)scrollView.Content).Margin.ToString();
	}

	private void BtnSetContentMargin_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Thickness margin = GetThicknessFromString(txtContentMargin.Text);

			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).Margin = margin;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				((FrameworkElement)wuxScrollViewer.Content).Margin = margin;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentPadding_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is Control || scrollView.Content is Border || scrollView.Content is StackPanel))
			txtContentPadding.Text = string.Empty;
		else if (scrollView.Content is Control)
			txtContentPadding.Text = ((Control)scrollView.Content).Padding.ToString();
		else if (scrollView.Content is Border)
			txtContentPadding.Text = ((Border)scrollView.Content).Padding.ToString();
		else
			txtContentPadding.Text = ((StackPanel)scrollView.Content).Padding.ToString();
	}

	private void BtnSetContentPadding_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Thickness padding = GetThicknessFromString(txtContentPadding.Text);

			if (scrollView.Content is Control)
			{
				((Control)scrollView.Content).Padding = padding;
			}
			else if (scrollView.Content is Border)
			{
				((Border)scrollView.Content).Padding = padding;
			}
			else if (scrollView.Content is StackPanel)
			{
				((StackPanel)scrollView.Content).Padding = padding;
			}

			if (wuxScrollViewer.Content is Control)
			{
				((Control)wuxScrollViewer.Content).Padding = padding;
			}
			else if (wuxScrollViewer.Content is Border)
			{
				((Border)wuxScrollViewer.Content).Padding = padding;
			}
			else if (wuxScrollViewer.Content is StackPanel)
			{
				((StackPanel)wuxScrollViewer.Content).Padding = padding;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentActualWidth_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentActualWidth.Text = string.Empty;
		else
		{
			txtContentActualWidth.Text = (scrollView.Content as FrameworkElement).ActualWidth.ToString();

			if ((scrollView.Content as FrameworkElement).ActualWidth != (wuxScrollViewer.Content as FrameworkElement).ActualWidth)
			{
				lstLogs.Items.Add($"muxScrollView.Content.ActualWidth={(scrollView.Content as FrameworkElement).ActualWidth} != wuxScrollViewer.Content.ActualWidth={(wuxScrollViewer.Content as FrameworkElement).ActualWidth}");
			}
		}
	}

	private void BtnGetContentActualHeight_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null || !(scrollView.Content is FrameworkElement))
			txtContentActualHeight.Text = string.Empty;
		else
		{
			txtContentActualHeight.Text = (scrollView.Content as FrameworkElement).ActualHeight.ToString();

			if ((scrollView.Content as FrameworkElement).ActualHeight != (wuxScrollViewer.Content as FrameworkElement).ActualHeight)
			{
				lstLogs.Items.Add($"muxScrollView.Content.ActualHeight={(scrollView.Content as FrameworkElement).ActualHeight} != wuxScrollViewer.Content.ActualHeight={(wuxScrollViewer.Content as FrameworkElement).ActualHeight}");
			}
		}
	}

	private void BtnGetContentDesiredSize_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null)
			txtContentDesiredSize.Text = string.Empty;
		else
		{
			txtContentDesiredSize.Text = scrollView.Content.DesiredSize.ToString();

			if (scrollView.Content.DesiredSize != (wuxScrollViewer.Content as UIElement).DesiredSize)
			{
				lstLogs.Items.Add($"muxScrollView.Content.DesiredSize={scrollView.Content.DesiredSize} != wuxScrollViewer.Content.DesiredSize={(wuxScrollViewer.Content as UIElement).DesiredSize}");
			}
		}
	}

	private void BtnGetContentRenderSize_Click(object sender, RoutedEventArgs e)
	{
		if (scrollView.Content == null)
			txtContentRenderSize.Text = string.Empty;
		else
		{
			txtContentRenderSize.Text = scrollView.Content.RenderSize.ToString();

			if (scrollView.Content.RenderSize != (wuxScrollViewer.Content as UIElement).RenderSize)
			{
				lstLogs.Items.Add($"muxScrollView.Content.RenderSize={scrollView.Content.RenderSize} != wuxScrollViewer.Content.RenderSize={(wuxScrollViewer.Content as UIElement).RenderSize}");
			}
		}
	}

	private void CmbContentHorizontalAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).HorizontalAlignment = (HorizontalAlignment)cmbContentHorizontalAlignment.SelectedIndex;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				wuxScrollViewer.HorizontalContentAlignment =
				((FrameworkElement)wuxScrollViewer.Content).HorizontalAlignment = (HorizontalAlignment)cmbContentHorizontalAlignment.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbContentVerticalAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (scrollView.Content is FrameworkElement)
			{
				((FrameworkElement)scrollView.Content).VerticalAlignment = (VerticalAlignment)cmbContentVerticalAlignment.SelectedIndex;
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				wuxScrollViewer.VerticalContentAlignment =
				((FrameworkElement)wuxScrollViewer.Content).VerticalAlignment = (VerticalAlignment)cmbContentVerticalAlignment.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbContentManipulationMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (scrollView.Content is FrameworkElement)
			{
				switch (cmbContentManipulationMode.SelectedIndex)
				{
					case 0:
						scrollView.Content.ManipulationMode = ManipulationModes.System;
						break;
					case 1:
						scrollView.Content.ManipulationMode = ManipulationModes.None;
						break;
				}
			}

			if (wuxScrollViewer.Content is FrameworkElement)
			{
				switch (cmbContentManipulationMode.SelectedIndex)
				{
					case 0:
						(wuxScrollViewer.Content as FrameworkElement).ManipulationMode = ManipulationModes.System;
						break;
					case 1:
						(wuxScrollViewer.Content as FrameworkElement).ManipulationMode = ManipulationModes.None;
						break;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void CmbSnapPointKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (cmbSnapPointAlignment != null)
		{
			cmbSnapPointAlignment.IsEnabled = cmbSnapPointKind.SelectedIndex != 2;
		}
	}

	private void BtnAddIrregularSnapPoint_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (scrollView != null)
			{
				ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

				if (scrollPresenter != null)
				{
					if (cmbSnapPointKind.SelectedIndex == 0)
					{
						ScrollSnapPoint snapPoint = new ScrollSnapPoint(
							Convert.ToSingle(txtIrregularSnapPointValue.Text),
							(ScrollSnapPointsAlignment)cmbSnapPointAlignment.SelectedIndex);
						if (cmbSnapPointKind.SelectedIndex == 0)
						{
							scrollPresenter.VerticalSnapPoints.Add(snapPoint);
						}
						else
						{
							scrollPresenter.HorizontalSnapPoints.Add(snapPoint);
						}
					}
					else if (cmbSnapPointKind.SelectedIndex == 2)
					{
						ZoomSnapPoint snapPoint = new ZoomSnapPoint(
							Convert.ToSingle(txtIrregularSnapPointValue.Text));
						scrollPresenter.ZoomSnapPoints.Add(snapPoint);
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnAddRepeatedSnapPoint_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (scrollView != null)
			{
				ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

				if (scrollPresenter != null)
				{
					if (cmbSnapPointKind.SelectedIndex == 0 || cmbSnapPointKind.SelectedIndex == 1)
					{
						RepeatedScrollSnapPoint snapPoint = new RepeatedScrollSnapPoint(
							Convert.ToDouble(txtRepeatedSnapPointOffset.Text),
							Convert.ToDouble(txtRepeatedSnapPointInterval.Text),
							Convert.ToDouble(txtRepeatedSnapPointStart.Text),
							Convert.ToDouble(txtRepeatedSnapPointEnd.Text),
							(ScrollSnapPointsAlignment)cmbSnapPointAlignment.SelectedIndex);
						if (cmbSnapPointKind.SelectedIndex == 0)
						{
							scrollPresenter.VerticalSnapPoints.Add(snapPoint);
						}
						else
						{
							scrollPresenter.HorizontalSnapPoints.Add(snapPoint);
						}
					}
					else if (cmbSnapPointKind.SelectedIndex == 2)
					{
						RepeatedZoomSnapPoint snapPoint = new RepeatedZoomSnapPoint(
							Convert.ToDouble(txtRepeatedSnapPointOffset.Text),
							Convert.ToDouble(txtRepeatedSnapPointInterval.Text),
							Convert.ToDouble(txtRepeatedSnapPointStart.Text),
							Convert.ToDouble(txtRepeatedSnapPointEnd.Text));
						scrollPresenter.ZoomSnapPoints.Add(snapPoint);
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnClearSnapPoints_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (scrollView != null)
			{
				ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

				if (scrollPresenter != null)
				{
					if (cmbSnapPointKind.SelectedIndex == 0)
					{
						scrollPresenter.VerticalSnapPoints.Clear();
					}
					else if (cmbSnapPointKind.SelectedIndex == 1)
					{
						scrollPresenter.HorizontalSnapPoints.Clear();
					}
					else if (cmbSnapPointKind.SelectedIndex == 2)
					{
						scrollPresenter.ZoomSnapPoints.Clear();
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbContentHorizontalAlignment()
	{
		try
		{
			if (scrollView.Content is FrameworkElement)
			{
				cmbContentHorizontalAlignment.SelectedIndex = (int)((FrameworkElement)scrollView.Content).HorizontalAlignment;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbContentVerticalAlignment()
	{
		try
		{
			if (scrollView.Content is FrameworkElement)
			{
				cmbContentVerticalAlignment.SelectedIndex = (int)((FrameworkElement)scrollView.Content).VerticalAlignment;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateCmbContentManipulationMode()
	{
		try
		{
			if (scrollView.Content != null)
			{
				switch (scrollView.Content.ManipulationMode)
				{
					case ManipulationModes.System:
						cmbContentManipulationMode.SelectedIndex = 0;
						break;
					case ManipulationModes.None:
						cmbContentManipulationMode.SelectedIndex = 1;
						break;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateContentOrientation()
	{
		try
		{
			cmbContentOrientation.SelectedIndex = (int)scrollView.ContentOrientation;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateHorizontalScrollMode()
	{
		try
		{
			cmbHorizontalScrollMode.SelectedIndex = (int)scrollView.HorizontalScrollMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateHorizontalScrollChainMode()
	{
		try
		{
			cmbHorizontalScrollChainMode.SelectedIndex = (int)scrollView.HorizontalScrollChainMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateHorizontalScrollRailMode()
	{
		try
		{
			cmbHorizontalScrollRailMode.SelectedIndex = (int)scrollView.HorizontalScrollRailMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateVerticalScrollMode()
	{
		try
		{
			cmbVerticalScrollMode.SelectedIndex = (int)scrollView.VerticalScrollMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateVerticalScrollChainMode()
	{
		try
		{
			cmbVerticalScrollChainMode.SelectedIndex = (int)scrollView.VerticalScrollChainMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateVerticalScrollRailMode()
	{
		try
		{
			cmbVerticalScrollRailMode.SelectedIndex = (int)scrollView.VerticalScrollRailMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateZoomMode()
	{
		try
		{
			cmbZoomMode.SelectedIndex = (int)scrollView.ZoomMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateMinZoomFactor()
	{
		try
		{
			txtMinZoomFactor.Text = scrollView.MinZoomFactor.ToString();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateMaxZoomFactor()
	{
		try
		{
			txtMaxZoomFactor.Text = scrollView.MaxZoomFactor.ToString();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateZoomChainMode()
	{
		try
		{
			cmbZoomChainMode.SelectedIndex = (int)scrollView.ZoomChainMode;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateIgnoredInputKinds()
	{
		try
		{
			switch (scrollView.IgnoredInputKinds)
			{
				case ScrollingInputKinds.None:
					cmbIgnoredInputKinds.SelectedIndex = 0;
					break;
				case ScrollingInputKinds.Touch:
					cmbIgnoredInputKinds.SelectedIndex = 1;
					break;
				case ScrollingInputKinds.Pen:
					cmbIgnoredInputKinds.SelectedIndex = 2;
					break;
				case ScrollingInputKinds.MouseWheel:
					cmbIgnoredInputKinds.SelectedIndex = 3;
					break;
				case ScrollingInputKinds.Keyboard:
					cmbIgnoredInputKinds.SelectedIndex = 4;
					break;
				case ScrollingInputKinds.Gamepad:
					cmbIgnoredInputKinds.SelectedIndex = 5;
					break;
				case ScrollingInputKinds.All:
					cmbIgnoredInputKinds.SelectedIndex = 6;
					break;
				default:
					lstLogs.Items.Add("Unexpected IgnoredInputKinds value.");
					break;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void UpdateWidth()
	{
		txtWidth.Text = scrollView.Width.ToString();
	}

	private void UpdateHeight()
	{
		txtHeight.Text = scrollView.Height.ToString();
	}

	private void UpdateBackground()
	{
		if (scrollView.Background == null)
		{
			cmbBackground.SelectedIndex = 0;
		}
		else if ((scrollView.Background as SolidColorBrush).Color == Windows.UI.Colors.Transparent)
		{
			cmbBackground.SelectedIndex = 1;
		}
		else if ((scrollView.Background as SolidColorBrush).Color == Windows.UI.Colors.AliceBlue)
		{
			cmbBackground.SelectedIndex = 2;
		}
		else if ((scrollView.Background as SolidColorBrush).Color == Windows.UI.Colors.Aqua)
		{
			cmbBackground.SelectedIndex = 3;
		}
	}

	private void UpdateContent()
	{
		if (scrollView.Content == null)
		{
			cmbContent.SelectedIndex = 0;
		}
		else if (scrollView.Content is Image)
		{
			if (((scrollView.Content as Image).Source as BitmapImage).UriSource.AbsolutePath.ToLower().Contains("large"))
			{
				cmbContent.SelectedIndex = 2;
			}
			else
			{
				cmbContent.SelectedIndex = 1;
			}
		}
		else if (scrollView.Content is Rectangle)
		{
			cmbContent.SelectedIndex = 3;
		}
		else if (scrollView.Content is Button)
		{
			cmbContent.SelectedIndex = 4;
		}
		else if (scrollView.Content is Border)
		{
			cmbContent.SelectedIndex = 5;
		}
		else if (scrollView.Content is StackPanel)
		{
			if ((scrollView.Content as StackPanel).Children.Count == 2)
			{
				cmbContent.SelectedIndex = 6;
			}
			else
			{
				cmbContent.SelectedIndex = 7;
			}
		}
		else if (scrollView.Content is Viewbox)
		{
			cmbContent.SelectedIndex = 8;
		}
	}

	private void UpdateMargin()
	{
		txtMargin.Text = scrollView.Margin.ToString();
	}

	private void UpdatePadding()
	{
		txtPadding.Text = scrollView.Padding.ToString();
	}

	private void CreateChildren()
	{
		largeImg = new Image() { Source = new BitmapImage(new Uri("ms-appx:/Assets/LargeWisteria.jpg")) };
		LinearGradientBrush lgb = new LinearGradientBrush() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
		GradientStop gs = new GradientStop() { Color = Windows.UI.Colors.Blue, Offset = 0.0 };
		lgb.GradientStops.Add(gs);
		gs = new GradientStop() { Color = Windows.UI.Colors.White, Offset = 0.5 };
		lgb.GradientStops.Add(gs);
		gs = new GradientStop() { Color = Windows.UI.Colors.Red, Offset = 1.0 };
		lgb.GradientStops.Add(gs);
		rectangle = new Rectangle() { Fill = lgb };
		rectangle.Name = "rect";
		button = new Button() { Content = "Button" };
		button.Name = "btn";
		Rectangle borderChild = new Rectangle() { Fill = lgb };
		border = new Border() { BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse), BorderThickness = new Thickness(5), Child = borderChild };
		border.Name = "bdr";

		sp1 = new StackPanel();
		sp1.Name = "sp1";
		Button button1 = new Button() { Content = "Button1" };
		button1.Name = "btn1";
		button1.Margin = new Thickness(50);
		Button button2 = new Button() { Content = "Button2" };
		button2.Name = "btn2";
		button2.Margin = new Thickness(50);
		sp1.Children.Add(button1);
		sp1.Children.Add(button2);

		sp2 = new StackPanel();
		sp2.Name = "sp2";
		sp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Indigo) });
		sp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Orange) });
		sp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Purple) });
		sp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Goldenrod) });

		viewbox = new Viewbox();
		viewbox.Name = "viewbox";
		Rectangle viewboxChild = new Rectangle() { Fill = lgb, Width = 600, Height = 400 };
		viewbox.Child = viewboxChild;

		wuxLargeImg = new Image() { Source = new BitmapImage(new Uri("ms-appx:/Assets/LargeWisteria.jpg")) };
		wuxRectangle = new Rectangle() { Fill = lgb };
		wuxRectangle.Name = "wuxRect";
		wuxButton = new Button() { Content = "Button" };
		wuxButton.Name = "wuxBtn";
		borderChild = new Rectangle() { Fill = lgb };
		wuxBorder = new Border() { BorderBrush = new SolidColorBrush(Windows.UI.Colors.Chartreuse), BorderThickness = new Thickness(5), Child = borderChild };
		wuxBorder.Name = "wuxBdr";

		wuxSp1 = new StackPanel();
		wuxSp1.Name = "wuxSp1";
		button1 = new Button() { Content = "Button1" };
		button1.Name = "wuxBtn1";
		button1.Margin = new Thickness(50);
		button2 = new Button() { Content = "Button2" };
		button2.Name = "wuxBtn2";
		button2.Margin = new Thickness(50);
		wuxSp1.Children.Add(button1);
		wuxSp1.Children.Add(button2);

		wuxSp2 = new StackPanel();
		wuxSp2.Name = "wuxSp2";
		wuxSp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Indigo) });
		wuxSp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Orange) });
		wuxSp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Purple) });
		wuxSp2.Children.Add(new Rectangle() { Height = 200, Fill = new SolidColorBrush(Windows.UI.Colors.Goldenrod) });

		wuxViewbox = new Viewbox();
		wuxViewbox.Name = "wuxViewbox";
		viewboxChild = new Rectangle() { Fill = lgb, Width = 600, Height = 400 };
		wuxViewbox.Child = viewboxChild;

		rootGrid.Children.RemoveAt(rootGrid.Children.Count - 1);
		rootGrid.Children.RemoveAt(rootGrid.Children.Count - 1);

		spH1.Visibility = Visibility.Visible;
		wuxSpH1.Visibility = Visibility.Visible;
	}

	private Thickness GetThicknessFromString(string thickness)
	{
		string[] lengths = thickness.Split(',');
		if (lengths.Length < 4)
			return new Thickness(Convert.ToDouble(lengths[0]));
		else
			return new Thickness(
				Convert.ToDouble(lengths[0]), Convert.ToDouble(lengths[1]), Convert.ToDouble(lengths[2]), Convert.ToDouble(lengths[3]));
	}

	private void UseScrollView(ScrollView sv2)
	{
		if (scrollView == sv2 || sv2 == null)
			return;

		try
		{
			//if (scrollView == null && (chkLogScrollViewMessages.IsChecked == true || chkLogScrollPresenterMessages.IsChecked == true))
			//{
			//    MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;

			//    if (chkLogScrollPresenterMessages.IsChecked == true)
			//    {
			//        MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//    }
			//    if (chkLogScrollViewMessages.IsChecked == true)
			//    {
			//        MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//    }
			//}

			if (scrollView != null)
			{
				scrollView.GettingFocus -= ScrollView_GettingFocus;
				scrollView.GotFocus -= ScrollView_GotFocus;
				scrollView.LosingFocus -= ScrollView_LosingFocus;
				scrollView.LostFocus -= ScrollView_LostFocus;

				if (chkLogScrollViewEvents.IsChecked == true)
				{
					scrollView.ExtentChanged -= ScrollView_ExtentChanged;
					scrollView.StateChanged -= ScrollView_StateChanged;
					scrollView.ViewChanged -= ScrollView_ViewChanged;
					scrollView.ScrollAnimationStarting -= ScrollView_ScrollAnimationStarting;
					scrollView.ZoomAnimationStarting -= ScrollView_ZoomAnimationStarting;
					if (scrollViewPointerWheelChangedEventHandler != null)
					{
						scrollView.RemoveHandler(UIElement.PointerWheelChangedEvent, scrollViewPointerWheelChangedEventHandler);
					}
				}

				ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

				if (scrollPresenter != null && chkLogScrollPresenterEvents.IsChecked == true)
				{
					scrollPresenter.ExtentChanged -= ScrollPresenter_ExtentChanged;
					scrollPresenter.StateChanged -= ScrollPresenter_StateChanged;
					scrollPresenter.ViewChanged -= ScrollPresenter_ViewChanged;
					scrollPresenter.ScrollAnimationStarting -= ScrollPresenter_ScrollAnimationStarting;
					scrollPresenter.ZoomAnimationStarting -= ScrollPresenter_ZoomAnimationStarting;
					if (scrollPresenterPointerWheelChangedEventHandler != null)
					{
						scrollPresenter.RemoveHandler(UIElement.PointerWheelChangedEvent, scrollPresenterPointerWheelChangedEventHandler);
					}
				}
			}

			scrollView = sv2;

			UpdateContentOrientation();
			UpdateHorizontalScrollMode();
			UpdateHorizontalScrollChainMode();
			UpdateHorizontalScrollRailMode();
			UpdateVerticalScrollMode();
			UpdateVerticalScrollChainMode();
			UpdateVerticalScrollRailMode();
			UpdateZoomMode();
			UpdateZoomChainMode();
			UpdateIgnoredInputKinds();
			UpdateMinZoomFactor();
			UpdateMaxZoomFactor();

			UpdateWidth();
			UpdateHeight();
			UpdateBackground();
			UpdateContent();
			UpdateMargin();
			UpdatePadding();

			UpdateCmbHorizontalScrollBarVisibility();
			UpdateCmbVerticalScrollBarVisibility();
			UpdateCmbXYFocusKeyboardNavigation();
			UpdateHorizontalAnchorRatio();
			UpdateVerticalAnchorRatio();
			UpdateHorizontalOffset();
			UpdateVerticalOffset();
			UpdateZoomFactor();
			UpdateExtentWidth();
			UpdateExtentHeight();
			UpdateTblComputedHorizontalScrollMode();
			UpdateTblComputedVerticalScrollMode();
			UpdateCmbContentHorizontalAlignment();
			UpdateCmbContentVerticalAlignment();
			UpdateCmbContentManipulationMode();

			chkIsEnabled.IsChecked = scrollView.IsEnabled;
			chkIsTabStop.IsChecked = scrollView.IsTabStop;

			if (scrollView != null)
			{
				scrollView.GettingFocus += ScrollView_GettingFocus;
				scrollView.GotFocus += ScrollView_GotFocus;
				scrollView.LosingFocus += ScrollView_LosingFocus;
				scrollView.LostFocus += ScrollView_LostFocus;

				if (chkLogScrollViewEvents.IsChecked == true)
				{
					scrollView.ExtentChanged += ScrollView_ExtentChanged;
					scrollView.StateChanged += ScrollView_StateChanged;
					scrollView.ViewChanged += ScrollView_ViewChanged;
					scrollView.ScrollAnimationStarting += ScrollView_ScrollAnimationStarting;
					scrollView.ZoomAnimationStarting += ScrollView_ZoomAnimationStarting;
					if (scrollViewPointerWheelChangedEventHandler == null)
					{
						scrollViewPointerWheelChangedEventHandler = new PointerEventHandler(ScrollView_PointerWheelChanged);
					}
					scrollView.AddHandler(UIElement.PointerWheelChangedEvent, scrollViewPointerWheelChangedEventHandler, true);
				}

				ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

				if (scrollPresenter != null && chkLogScrollPresenterEvents.IsChecked == true)
				{
					scrollPresenter.ExtentChanged += ScrollPresenter_ExtentChanged;
					scrollPresenter.StateChanged += ScrollPresenter_StateChanged;
					scrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
					scrollPresenter.ScrollAnimationStarting += ScrollPresenter_ScrollAnimationStarting;
					scrollPresenter.ZoomAnimationStarting += ScrollPresenter_ZoomAnimationStarting;
					if (scrollPresenterPointerWheelChangedEventHandler == null)
					{
						scrollPresenterPointerWheelChangedEventHandler = new PointerEventHandler(ScrollPresenter_PointerWheelChanged);
					}
					scrollPresenter.AddHandler(UIElement.PointerWheelChangedEvent, scrollPresenterPointerWheelChangedEventHandler, true);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private Windows.UI.Xaml.Controls.ScrollBarVisibility MuxScrollBarVisibilityToWuxScrollBarVisibility(ScrollingScrollBarVisibility muxScrollBarVisibility)
	{
		switch (muxScrollBarVisibility)
		{
			case ScrollingScrollBarVisibility.Auto:
				return Windows.UI.Xaml.Controls.ScrollBarVisibility.Auto;
			case ScrollingScrollBarVisibility.Hidden:
				return Windows.UI.Xaml.Controls.ScrollBarVisibility.Hidden;
			default:
				return Windows.UI.Xaml.Controls.ScrollBarVisibility.Visible;
		}
	}

	private Windows.UI.Xaml.Controls.ScrollMode MuxScrollModeToWuxScrollMode(ScrollingScrollMode muxScrollMode)
	{
		switch (muxScrollMode)
		{
			case ScrollingScrollMode.Disabled:
				return Windows.UI.Xaml.Controls.ScrollMode.Disabled;
			default:
				return Windows.UI.Xaml.Controls.ScrollMode.Enabled;
		}
	}

	private Windows.UI.Xaml.Controls.ZoomMode MuxZoomModeToWuxZoomMode(ScrollingZoomMode muxZoomMode)
	{
		switch (muxZoomMode)
		{
			case ScrollingZoomMode.Disabled:
				return Windows.UI.Xaml.Controls.ZoomMode.Disabled;
			default:
				return Windows.UI.Xaml.Controls.ZoomMode.Enabled;
		}
	}

	private bool MuxRailModeToWuxRailMode(ScrollingRailMode muxRailingMode)
	{
		switch (muxRailingMode)
		{
			case ScrollingRailMode.Disabled:
				return false;
			default:
				return true;
		}
	}

	private void Content_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
	{
		AppendAsyncEventMessage($"Content.PointerWheelChanged args.Handled={args.Handled}");
	}

	private void ScrollView_GettingFocus(UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage("ScrollView.GettingFocus FocusState=" + args.FocusState + ", Direction=" + args.Direction + ", InputDevice=" + args.InputDevice + ", oldFE=" + oldFEName + ", newFE=" + newFEName);
	}

	private void ScrollView_LostFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ScrollView.LostFocus");
	}

	private void ScrollView_LosingFocus(UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage("ScrollView.LosingFocus FocusState=" + args.FocusState + ", Direction=" + args.Direction + ", InputDevice=" + args.InputDevice + ", oldFE=" + oldFEName + ", newFE=" + newFEName);
	}

	private void ScrollView_GotFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ScrollView.GotFocus");
	}

	private void ScrollPresenter_ExtentChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ExtentChanged ExtentWidth=" + sender.ExtentWidth + ", ExtentHeight=" + sender.ExtentHeight);
	}

	private void ScrollPresenter_StateChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.StateChanged " + sender.State.ToString());
	}

	private void ScrollPresenter_ViewChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ViewChanged H=" + sender.HorizontalOffset.ToString() + ", V=" + sender.VerticalOffset + ", ZF=" + sender.ZoomFactor);
	}

	private void ScrollPresenter_ScrollAnimationStarting(ScrollPresenter sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ScrollAnimationStarting OffsetsChangeCorrelationId=" + args.CorrelationId + " SP=(" + args.StartPosition.X + "," + args.StartPosition.Y + ") EP=(" + args.EndPosition.X + "," + args.EndPosition.Y + ")");
	}

	private void ScrollPresenter_ZoomAnimationStarting(ScrollPresenter sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ZoomAnimationStarting ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", CenterPoint=" + args.CenterPoint + ", SZF=" + args.StartZoomFactor + ", EZF=" + args.EndZoomFactor);
	}

	private void ScrollPresenter_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.PointerWheelChanged args.Handled={args.Handled}");
	}

	private void ScrollView_ExtentChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.ExtentChanged ExtentWidth=" + sender.ExtentWidth + ", ExtentHeight=" + sender.ExtentHeight);
	}

	private void ScrollView_StateChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.StateChanged " + sender.State.ToString());
	}

	private void ScrollView_ViewChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.ViewChanged H=" + sender.HorizontalOffset.ToString() + ", V=" + sender.VerticalOffset + ", ZF=" + sender.ZoomFactor);
	}

	private void ScrollView_ScrollAnimationStarting(ScrollView sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollView.ScrollAnimationStarting OffsetsChangeCorrelationId=" + args.CorrelationId);
	}

	private void ScrollView_ZoomAnimationStarting(ScrollView sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollView.ZoomAnimationStarting ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", CenterPoint=" + args.CenterPoint);
	}

	private void ScrollView_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.PointerWheelChanged args.Handled={args.Handled}");
	}

	private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
	{
		lstLogs.Items.Clear();
	}

	private void ChkLogContentEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				UIElement content = scrollPresenter.Content;

				if (content != null)
				{
					if (contentPointerWheelChangedEventHandler == null)
					{
						contentPointerWheelChangedEventHandler = new PointerEventHandler(Content_PointerWheelChanged);
					}

					content.AddHandler(UIElement.PointerWheelChangedEvent, contentPointerWheelChangedEventHandler, true);
				}
			}
		}
	}

	private void ChkLogContentEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null && contentPointerWheelChangedEventHandler != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				UIElement content = scrollPresenter.Content;

				if (content != null)
				{
					content.RemoveHandler(UIElement.PointerWheelChangedEvent, contentPointerWheelChangedEventHandler);
				}
			}
		}
	}

	private void ChkLogScrollPresenterEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.ExtentChanged += ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged += ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting += ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting += ScrollPresenter_ZoomAnimationStarting;
				if (scrollPresenterPointerWheelChangedEventHandler == null)
				{
					scrollPresenterPointerWheelChangedEventHandler = new PointerEventHandler(ScrollPresenter_PointerWheelChanged);
				}
				scrollPresenter.AddHandler(UIElement.PointerWheelChangedEvent, scrollPresenterPointerWheelChangedEventHandler, true);
			}
		}
	}

	private void ChkLogScrollPresenterEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.ExtentChanged -= ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged -= ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged -= ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting -= ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting -= ScrollPresenter_ZoomAnimationStarting;
				if (scrollPresenterPointerWheelChangedEventHandler != null)
				{
					scrollPresenter.RemoveHandler(UIElement.PointerWheelChangedEvent, scrollPresenterPointerWheelChangedEventHandler);
				}
			}
		}
	}

	private void ChkLogScrollViewEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			scrollView.ExtentChanged += ScrollView_ExtentChanged;
			scrollView.StateChanged += ScrollView_StateChanged;
			scrollView.ViewChanged += ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting += ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting += ScrollView_ZoomAnimationStarting;
			if (scrollViewPointerWheelChangedEventHandler == null)
			{
				scrollViewPointerWheelChangedEventHandler = new PointerEventHandler(ScrollView_PointerWheelChanged);
			}
			scrollView.AddHandler(UIElement.PointerWheelChangedEvent, scrollViewPointerWheelChangedEventHandler, true);
		}
	}

	private void ChkLogScrollViewEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			scrollView.ExtentChanged -= ScrollView_ExtentChanged;
			scrollView.StateChanged -= ScrollView_StateChanged;
			scrollView.ViewChanged -= ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting -= ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting -= ScrollView_ZoomAnimationStarting;
			if (scrollViewPointerWheelChangedEventHandler != null)
			{
				scrollView.RemoveHandler(UIElement.PointerWheelChangedEvent, scrollViewPointerWheelChangedEventHandler);
			}
		}
	}

	private void ChkLogScrollPresenterMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//    MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollPresenterMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//    MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//    MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//    MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkAutoHideScrollControllers_Indeterminate(object sender, RoutedEventArgs e)
	{
		ScrollViewTestHooks.SetAutoHideScrollControllers(scrollView, null);
	}

	private void ChkAutoHideScrollControllers_Checked(object sender, RoutedEventArgs e)
	{
		ScrollViewTestHooks.SetAutoHideScrollControllers(scrollView, true);
	}

	private void ChkAutoHideScrollControllers_Unchecked(object sender, RoutedEventArgs e)
	{
		ScrollViewTestHooks.SetAutoHideScrollControllers(scrollView, false);
	}

	//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//    // Cut off the terminating new line.
	//    string msg = args.Message.Substring(0, args.Message.Length - 1);
	//    string asyncEventMessage = string.Empty;
	//    string senderName = string.Empty;

	//    try
	//    {
	//        FrameworkElement fe = sender as FrameworkElement;

	//        if (fe != null)
	//        {
	//            senderName = "s:" + fe.Name + ", ";
	//        }
	//    }
	//    catch
	//    {
	//    }

	//    if (args.IsVerboseLevel)
	//    {
	//        asyncEventMessage = "Verbose: " + senderName + "m:" + msg;
	//    }
	//    else
	//    {
	//        asyncEventMessage = "Info: " + senderName + "m:" + msg;
	//    }

	//    AppendAsyncEventMessage(asyncEventMessage);
	//}

	private void AppendAsyncEventMessage(string asyncEventMessage)
	{
		lock (asyncEventReportingLock)
		{
			while (asyncEventMessage.Length > 0)
			{
				string msgHead = asyncEventMessage;

				if (asyncEventMessage.Length > 110)
				{
					int commaIndex = asyncEventMessage.IndexOf(',', 110);
					if (commaIndex != -1)
					{
						msgHead = asyncEventMessage.Substring(0, commaIndex);
						asyncEventMessage = asyncEventMessage.Substring(commaIndex + 1);
					}
					else
					{
						asyncEventMessage = string.Empty;
					}
				}
				else
				{
					asyncEventMessage = string.Empty;
				}

				lstAsyncEventMessage.Add(msgHead);
			}

			var ignored = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, AppendAsyncEventMessage);
		}
	}

	private void AppendAsyncEventMessage()
	{
		lock (asyncEventReportingLock)
		{
			foreach (string asyncEventMessage in lstAsyncEventMessage)
			{
				lstLogs.Items.Add(asyncEventMessage);
			}
			lstAsyncEventMessage.Clear();
		}
	}

	private void BtnClearExceptionReport_Click(object sender, RoutedEventArgs e)
	{
		txtExceptionReport.Text = string.Empty;
	}
}
