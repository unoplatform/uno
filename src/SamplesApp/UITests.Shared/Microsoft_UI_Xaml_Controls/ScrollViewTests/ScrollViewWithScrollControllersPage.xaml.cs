// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Private.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewWithScrollControllersPage : TestPage
{
	private Object asyncEventReportingLock = new Object();
	private List<string> lstAsyncEventMessage = new List<string>();
	private ScrollView scrollView = null;

	public ScrollViewWithScrollControllersPage()
	{
		// We need the styles of the CompositionScrollController, so lets load it
		App.AppendResourceDictionaryToMergedDictionaries(App.AdditionStylesXaml);

		this.InitializeComponent();
		UseScrollView(this.markupScrollView);
	}

	~ScrollViewWithScrollControllersPage()
	{
	}

	//protected override void OnNavigatedFrom(NavigationEventArgs e)
	//{
	//	MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
	//	MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

	//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

	//	base.OnNavigatedFrom(e);
	//}

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

	private void ChkScrollPresenterAttachedProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (grdScrollPresenterAttachedProperties != null)
			grdScrollPresenterAttachedProperties.Visibility = Visibility.Visible;
	}

	private void ChkScrollPresenterAttachedProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdScrollPresenterAttachedProperties != null)
			grdScrollPresenterAttachedProperties.Visibility = Visibility.Collapsed;
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

	private void BtnGetVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollMode();
	}

	private void BtnGetZoomMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomMode();
	}

	private void BtnSetContentOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScrollingContentOrientation co = (ScrollingContentOrientation)cmbContentOrientation.SelectedIndex;
			scrollView.ContentOrientation = co;
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
			ScrollingZoomMode ssm = (ScrollingZoomMode)cmbZoomMode.SelectedIndex;
			scrollView.ZoomMode = ssm;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
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
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetHorizontalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollBarVisibility();
	}

	private void BtnSetHorizontalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.HorizontalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbHorizontalScrollBarVisibility.SelectedIndex;
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetVerticalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollBarVisibility();
	}

	private void BtnSetVerticalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			scrollView.VerticalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbVerticalScrollBarVisibility.SelectedIndex;
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
	}

	private void ChkIsEnabled_Unchecked(object sender, RoutedEventArgs e)
	{
		scrollView.IsEnabled = false;
	}

	private void ChkIsTabStop_Checked(object sender, RoutedEventArgs e)
	{
		scrollView.IsTabStop = true;
	}

	private void ChkIsTabStop_Unchecked(object sender, RoutedEventArgs e)
	{
		scrollView.IsTabStop = false;
	}

	private void BtnGetContentWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateContentWidth();
	}

	private void BtnSetContentWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
			if (contentAsFE != null)
				contentAsFE.Width = Convert.ToDouble(txtContentWidth.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateContentHeight();
	}

	private void BtnSetContentHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
			if (contentAsFE != null)
				contentAsFE.Height = Convert.ToDouble(txtContentHeight.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnGetContentMargin_Click(object sender, RoutedEventArgs e)
	{
		UpdateContentMargin();
	}

	private void BtnSetContentMargin_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
			if (contentAsFE != null)
				contentAsFE.Margin = GetThicknessFromString(txtContentMargin.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void ChkIsContentEnabled_Checked(object sender, RoutedEventArgs e)
	{
		Control contentAsC = scrollView.Content as Control;
		if (contentAsC != null)
			contentAsC.IsEnabled = true;
	}

	private void ChkIsContentEnabled_Unchecked(object sender, RoutedEventArgs e)
	{
		Control contentAsC = scrollView.Content as Control;
		if (contentAsC != null)
			contentAsC.IsEnabled = false;
	}

	private void ChkIsContentTabStop_Checked(object sender, RoutedEventArgs e)
	{
		Control contentAsC = scrollView.Content as Control;
		if (contentAsC != null)
			contentAsC.IsTabStop = true;
	}

	private void ChkIsContentTabStop_Unchecked(object sender, RoutedEventArgs e)
	{
		Control contentAsC = scrollView.Content as Control;
		if (contentAsC != null)
			contentAsC.IsTabStop = false;
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

	private void UpdateWidth()
	{
		txtWidth.Text = scrollView.Width.ToString();
	}

	private void UpdateHeight()
	{
		txtHeight.Text = scrollView.Height.ToString();
	}

	private void UpdatePadding()
	{
		txtPadding.Text = scrollView.Padding.ToString();
	}

	private void UpdateHorizontalScrollBarVisibility()
	{
		cmbHorizontalScrollBarVisibility.SelectedIndex = (int)scrollView.HorizontalScrollBarVisibility;
	}

	private void UpdateVerticalScrollBarVisibility()
	{
		cmbVerticalScrollBarVisibility.SelectedIndex = (int)scrollView.VerticalScrollBarVisibility;
	}

	private void UpdateContentWidth()
	{
		FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
		if (contentAsFE == null)
			txtContentWidth.Text = string.Empty;
		else
			txtContentWidth.Text = contentAsFE.Width.ToString();
	}

	private void UpdateContentHeight()
	{
		FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
		if (contentAsFE == null)
			txtContentHeight.Text = string.Empty;
		else
			txtContentHeight.Text = contentAsFE.Height.ToString();
	}

	private void UpdateContentMargin()
	{
		FrameworkElement contentAsFE = scrollView.Content as FrameworkElement;
		if (contentAsFE == null)
			txtContentMargin.Text = string.Empty;
		else
			txtContentMargin.Text = contentAsFE.Margin.ToString();
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
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;

			//	if (chkLogScrollPresenterMessages.IsChecked == true)
			//	{
			//		MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//	}
			//	if (chkLogScrollViewMessages.IsChecked == true)
			//	{
			//		MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//	}
			//}

			scrollView = sv2;

			UpdateContentOrientation();
			UpdateHorizontalScrollMode();
			UpdateVerticalScrollMode();
			UpdateZoomMode();

			UpdateWidth();
			UpdateHeight();
			UpdatePadding();
			UpdateHorizontalScrollBarVisibility();
			UpdateVerticalScrollBarVisibility();

			chkIsEnabled.IsChecked = scrollView.IsEnabled;
			chkIsTabStop.IsChecked = scrollView.IsTabStop;

			UpdateContentWidth();
			UpdateContentHeight();
			UpdateContentMargin();

			Control contentAsC = scrollView.Content as Control;
			if (contentAsC != null)
			{
				chkIsContentEnabled.IsChecked = contentAsC.IsEnabled;
				chkIsContentTabStop.IsChecked = contentAsC.IsTabStop;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			lstLogs.Items.Add(ex.ToString());
		}
	}

	private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
	{
		lstLogs.Items.Clear();
	}

	private void ChkLogScrollPresenterMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollPresenterMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//	// Cut off the terminating new line.
	//	string msg = args.Message.Substring(0, args.Message.Length - 1);
	//	string asyncEventMessage = string.Empty;
	//	string senderName = string.Empty;

	//	try
	//	{
	//		FrameworkElement fe = sender as FrameworkElement;

	//		if (fe != null)
	//		{
	//			senderName = "s:" + fe.Name + ", ";
	//		}
	//	}
	//	catch
	//	{
	//	}

	//	if (args.IsVerboseLevel)
	//	{
	//		asyncEventMessage = "Verbose: " + senderName + "m:" + msg;
	//	}
	//	else
	//	{
	//		asyncEventMessage = "Info: " + senderName + "m:" + msg;
	//	}

	//	lock (asyncEventReportingLock)
	//	{
	//		lstAsyncEventMessage.Add(asyncEventMessage);

	//		var ignored = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
	//													  AppendAsyncEventMessage);
	//	}
	//}

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
