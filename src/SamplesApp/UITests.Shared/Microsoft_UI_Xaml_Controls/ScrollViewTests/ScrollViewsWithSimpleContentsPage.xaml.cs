// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using ScrollingInputKinds = Windows.UI.Xaml.Controls.ScrollingInputKinds;
using ScrollPresenter = Windows.UI.Xaml.Controls.Primitives.ScrollPresenter;
using ScrollView = Windows.UI.Xaml.Controls.ScrollView;
using ScrollingScrollCompletedEventArgs = Windows.UI.Xaml.Controls.ScrollingScrollCompletedEventArgs;
using ScrollingZoomCompletedEventArgs = Windows.UI.Xaml.Controls.ScrollingZoomCompletedEventArgs;
using ScrollingScrollOptions = Windows.UI.Xaml.Controls.ScrollingScrollOptions;
using ScrollingZoomOptions = Windows.UI.Xaml.Controls.ScrollingZoomOptions;
using ScrollingAnimationMode = Windows.UI.Xaml.Controls.ScrollingAnimationMode;
using ScrollingSnapPointsMode = Windows.UI.Xaml.Controls.ScrollingSnapPointsMode;

using ScrollPresenterTestHooks = Microsoft.UI.Private.Controls.ScrollPresenterTestHooks;
using ScrollPresenterViewChangeResult = Microsoft.UI.Private.Controls.ScrollPresenterViewChangeResult;
using ScrollViewTestHooks = Microsoft.UI.Private.Controls.ScrollViewTestHooks;
using Uno.UI.Samples.Controls;
//using MUXControlsTestHooks = Microsoft.UI.Private.Controls.MUXControlsTestHooks;
//using MUXControlsTestHooksLoggingMessageEventArgs = Microsoft.UI.Private.Controls.MUXControlsTestHooksLoggingMessageEventArgs;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewsWithSimpleContentsPage : TestPage
{
	private List<string> fullLogs = new List<string>();
	private int scrollView52ZoomFactorChangeCorrelationId = -1;

	public ScrollViewsWithSimpleContentsPage()
	{
		this.InitializeComponent();

		this.scrollView51.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

		Loaded += ScrollViewsWithSimpleContentsPage_Loaded;
		KeyDown += ScrollViewsWithSimpleContentsPage_KeyDown;
	}

	~ScrollViewsWithSimpleContentsPage()
	{
	}

	//protected override void OnNavigatedFrom(NavigationEventArgs e)
	//{
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

	//    MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

	//    base.OnNavigatedFrom(e);
	//}

	private void ScrollViewsWithSimpleContentsPage_Loaded(object sender, RoutedEventArgs e)
	{
		this.scrollView11.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView21.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView31.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView41.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView51.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView11.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView21.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView31.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView41.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView51.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView11.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView21.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView31.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView41.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView51.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView11.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView21.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView31.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView41.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView51.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;

		this.scrollView12.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView22.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView32.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView42.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView52.ScrollPresenter.StateChanged += ScrollPresenter_StateChanged;
		this.scrollView12.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView22.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView32.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView42.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView52.ScrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
		this.scrollView12.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView22.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView32.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView42.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView52.ScrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
		this.scrollView12.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView22.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView32.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView42.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
		this.scrollView52.ScrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
	}

	private void ScrollViewsWithSimpleContentsPage_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.G)
		{
			GetFullLog();
		}
		else if (e.Key == Windows.System.VirtualKey.C)
		{
			ClearFullLog();
		}
	}

	private void ChkLogScrollPresenterMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	type: "ScrollPresenter",
		//	isLoggingInfoLevel: true,
		//	isLoggingVerboseLevel: true);

		//MUXControlsTestHooks.SetLoggingLevelForType(
		//	type: "ScrollPresenter",
		//	isLoggingInfoLevel: true,
		//	isLoggingVerboseLevel: true);

		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollPresenterMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	type: "ScrollPresenter",
		//	isLoggingInfoLevel: false,
		//	isLoggingVerboseLevel: false);

		//MUXControlsTestHooks.SetLoggingLevelForType(
		//	type: "ScrollPresenter",
		//	isLoggingInfoLevel: false,
		//	isLoggingVerboseLevel: false);

		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	type: "ScrollView",
		//	isLoggingInfoLevel: true,
		//	isLoggingVerboseLevel: true);

		//MUXControlsTestHooks.SetLoggingLevelForType(
		//	type: "ScrollView",
		//	isLoggingInfoLevel: true,
		//	isLoggingVerboseLevel: true);

		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	type: "ScrollView",
		//	isLoggingInfoLevel: false,
		//	isLoggingVerboseLevel: false);

		//MUXControlsTestHooks.SetLoggingLevelForType(
		//	type: "ScrollView",
		//	isLoggingInfoLevel: false,
		//	isLoggingVerboseLevel: false);

		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ScrollPresenter_StateChanged(ScrollPresenter sender, object args)
	{
		string senderId = "." + sender.Name;
		FrameworkElement parent = VisualTreeHelper.GetParent(sender) as FrameworkElement;
		if (parent != null)
		{
			senderId = "." + parent.Name + senderId;
			FrameworkElement grandParent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
			if (grandParent != null)
			{
				senderId = grandParent.Name + senderId;
			}
		}
		this.txtScrollPresenterState.Text = senderId + " " + sender.State.ToString();
		this.fullLogs.Add(senderId + " StateChanged S=" + sender.State.ToString());
		chkLogUpdated.IsChecked = false;
	}

	private void ScrollPresenter_ViewChanged(ScrollPresenter sender, object args)
	{
		string senderId = "." + sender.Name;
		FrameworkElement parent = VisualTreeHelper.GetParent(sender) as FrameworkElement;
		if (parent != null)
		{
			senderId = parent.Name + senderId;
		}
		this.txtScrollPresenterHorizontalOffset.Text = sender.HorizontalOffset.ToString();
		this.txtScrollPresenterVerticalOffset.Text = sender.VerticalOffset.ToString();
		this.txtScrollPresenterZoomFactor.Text = sender.ZoomFactor.ToString();
		this.fullLogs.Add(senderId + " ViewChanged H=" + this.txtScrollPresenterHorizontalOffset.Text + ", V=" + this.txtScrollPresenterVerticalOffset.Text + ", ZF=" + this.txtScrollPresenterZoomFactor.Text);
		chkLogUpdated.IsChecked = false;
	}

	private void ScrollPresenter_ScrollCompleted(ScrollPresenter sender, ScrollingScrollCompletedEventArgs args)
	{
		string senderId = "." + sender.Name;
		FrameworkElement parent = VisualTreeHelper.GetParent(sender) as FrameworkElement;
		if (parent != null)
		{
			senderId = parent.Name + senderId;
		}

		ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetScrollCompletedResult(args);

		this.fullLogs.Add(senderId + " ScrollCompleted. OffsetsChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
		chkLogUpdated.IsChecked = false;
	}

	private void ScrollPresenter_ZoomCompleted(ScrollPresenter sender, ScrollingZoomCompletedEventArgs args)
	{
		string senderId = "." + sender.Name;
		FrameworkElement parent = VisualTreeHelper.GetParent(sender) as FrameworkElement;
		if (parent != null)
		{
			senderId = parent.Name + senderId;
		}

		ScrollPresenterViewChangeResult result = ScrollPresenterTestHooks.GetZoomCompletedResult(args);

		this.fullLogs.Add(senderId + " ZoomCompleted. ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", Result=" + result);
		chkLogUpdated.IsChecked = false;

		if (args.CorrelationId == scrollView52ZoomFactorChangeCorrelationId)
		{
			this.txtResetStatus.Text = "Views reset";
			scrollView52ZoomFactorChangeCorrelationId = -1;
		}
	}

	private void CmbShowScrollView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (this.scrollView11 != null)
		{
			if (cmbShowScrollView.SelectedIndex == 0)
			{
				this.scrollView11.Visibility = Visibility.Visible;
				this.scrollView21.Visibility = Visibility.Visible;
				this.scrollView31.Visibility = Visibility.Visible;
				this.scrollView41.Visibility = Visibility.Visible;
				this.scrollView51.Visibility = Visibility.Visible;
				this.scrollView12.Visibility = Visibility.Visible;
				this.scrollView22.Visibility = Visibility.Visible;
				this.scrollView32.Visibility = Visibility.Visible;
				this.scrollView42.Visibility = Visibility.Visible;
				this.scrollView52.Visibility = Visibility.Visible;

				this.scrollView11.Width = double.NaN;
				this.scrollView21.Width = double.NaN;
				this.scrollView31.Width = double.NaN;
				this.scrollView41.Width = double.NaN;
				this.scrollView51.Width = double.NaN;
				this.scrollView12.Width = double.NaN;
				this.scrollView22.Width = double.NaN;
				this.scrollView32.Width = double.NaN;
				this.scrollView42.Width = double.NaN;
				this.scrollView52.Width = double.NaN;
				this.scrollView11.Height = double.NaN;
				this.scrollView21.Height = double.NaN;
				this.scrollView31.Height = double.NaN;
				this.scrollView41.Height = double.NaN;
				this.scrollView51.Height = double.NaN;
				this.scrollView12.Height = double.NaN;
				this.scrollView22.Height = double.NaN;
				this.scrollView32.Height = double.NaN;
				this.scrollView42.Height = double.NaN;
				this.scrollView52.Height = double.NaN;

				for (int rowIndex = 2; rowIndex < 4; rowIndex++)
					this.rootGrid.RowDefinitions[rowIndex].Height = new GridLength(1, GridUnitType.Star);

				for (int columnIndex = 0; columnIndex < 5; columnIndex++)
					this.rootGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);

				cmbIgnoredInputKinds.IsEnabled = false;
				cmbIgnoredInputKinds.SelectedIndex = 0;
			}
			else
			{
				this.scrollView11.Visibility = Visibility.Collapsed;
				this.scrollView21.Visibility = Visibility.Collapsed;
				this.scrollView31.Visibility = Visibility.Collapsed;
				this.scrollView41.Visibility = Visibility.Collapsed;
				this.scrollView51.Visibility = Visibility.Collapsed;
				this.scrollView12.Visibility = Visibility.Collapsed;
				this.scrollView22.Visibility = Visibility.Collapsed;
				this.scrollView32.Visibility = Visibility.Collapsed;
				this.scrollView42.Visibility = Visibility.Collapsed;
				this.scrollView52.Visibility = Visibility.Collapsed;

				for (int rowIndex = 2; rowIndex < 4; rowIndex++)
					this.rootGrid.RowDefinitions[rowIndex].Height = GridLength.Auto;

				for (int columnIndex = 0; columnIndex < 5; columnIndex++)
					this.rootGrid.ColumnDefinitions[columnIndex].Width = GridLength.Auto;

				cmbIgnoredInputKinds.IsEnabled = true;

				ScrollView scrollView = SelectedScrollView;

				scrollView.Visibility = Visibility.Visible;
				scrollView.Width = 300;
				scrollView.Height = 400;

				txtScrollPresenterHorizontalOffset.Text = scrollView.HorizontalOffset.ToString();
				txtScrollPresenterVerticalOffset.Text = scrollView.VerticalOffset.ToString();
				txtScrollPresenterZoomFactor.Text = scrollView.ZoomFactor.ToString();

				switch (scrollView.IgnoredInputKinds)
				{
					case ScrollingInputKinds.None:
						cmbIgnoredInputKinds.SelectedIndex = 1;
						break;
					case ScrollingInputKinds.Touch:
						cmbIgnoredInputKinds.SelectedIndex = 2;
						break;
					case ScrollingInputKinds.Pen:
						cmbIgnoredInputKinds.SelectedIndex = 3;
						break;
					case ScrollingInputKinds.MouseWheel:
						cmbIgnoredInputKinds.SelectedIndex = 4;
						break;
					case ScrollingInputKinds.Keyboard:
						cmbIgnoredInputKinds.SelectedIndex = 5;
						break;
					case ScrollingInputKinds.Gamepad:
						cmbIgnoredInputKinds.SelectedIndex = 6;
						break;
					case ScrollingInputKinds.All:
						cmbIgnoredInputKinds.SelectedIndex = 7;
						break;
					default:
						cmbIgnoredInputKinds.SelectedIndex = 0;
						break;
				}
			}
		}
	}

	private void CmbIgnoredInputKinds_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ScrollingInputKinds ignoredInputKinds;
		ScrollView scrollView = SelectedScrollView;

		switch (cmbIgnoredInputKinds.SelectedIndex)
		{
			case 0:
				return;
			case 1:
				ignoredInputKinds = ScrollingInputKinds.None;
				break;
			case 2:
				ignoredInputKinds = ScrollingInputKinds.Touch;
				break;
			case 3:
				ignoredInputKinds = ScrollingInputKinds.Pen;
				break;
			case 4:
				ignoredInputKinds = ScrollingInputKinds.MouseWheel;
				break;
			case 5:
				ignoredInputKinds = ScrollingInputKinds.Keyboard;
				break;
			case 6:
				ignoredInputKinds = ScrollingInputKinds.Gamepad;
				break;
			default:
				ignoredInputKinds = ScrollingInputKinds.All;
				break;
		}

		scrollView.IgnoredInputKinds = ignoredInputKinds;
	}

	private void btnGetFullLog_Click(object sender, RoutedEventArgs e)
	{
		GetFullLog();
	}

	private void btnClearFullLog_Click(object sender, RoutedEventArgs e)
	{
		ClearFullLog();
	}

	private void btnResetViews_Click(object sender, RoutedEventArgs e)
	{
		this.txtResetStatus.Text = "Resetting views ...";
		ResetView(this.scrollView11);
		ResetView(this.scrollView21);
		ResetView(this.scrollView31);
		ResetView(this.scrollView41);
		ResetView(this.scrollView51);
		ResetView(this.scrollView12);
		ResetView(this.scrollView22);
		ResetView(this.scrollView32);
		ResetView(this.scrollView42);
		ResetView(this.scrollView52);
	}

	private void GetFullLog()
	{
		this.txtResetStatus.Text = "GetFullLog. Populating cmbFullLog...";
		chkLogCleared.IsChecked = false;
		foreach (string log in this.fullLogs)
		{
			this.cmbFullLog.Items.Add(log);
		}
		chkLogUpdated.IsChecked = true;
		this.txtResetStatus.Text = "GetFullLog. Done.";
	}

	private void ClearFullLog()
	{
		this.txtResetStatus.Text = "ClearFullLog. Clearing cmbFullLog & fullLogs...";
		chkLogUpdated.IsChecked = false;
		this.fullLogs.Clear();
		this.cmbFullLog.Items.Clear();
		chkLogCleared.IsChecked = true;
		this.txtResetStatus.Text = "ClearFullLog. Done.";
	}

	//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//	// Cut off the terminating new line.
	//	string msg = args.Message.Substring(0, args.Message.Length - 1);
	//	string senderName = string.Empty;
	//	FrameworkElement fe = sender as FrameworkElement;

	//	if (fe != null)
	//	{
	//		senderName = "s:" + fe.Name + ", ";
	//	}

	//	fullLogs.Add((args.IsVerboseLevel ? "Verbose: " : "Info: ") + senderName + "m:" + msg);
	//}

	private void ResetView(ScrollView scrollView)
	{
		ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;
		string scrollPresenterId = (VisualTreeHelper.GetParent(scrollPresenter) as FrameworkElement).Name + "." + scrollPresenter.Name;

		int viewChangeCorrelationId = scrollPresenter.ScrollTo(0.0, 0.0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
		this.fullLogs.Add(scrollPresenterId + " ScrollTo requested. Id=" + viewChangeCorrelationId);

		viewChangeCorrelationId = scrollPresenter.ZoomTo(1.0f, System.Numerics.Vector2.Zero, new ScrollingZoomOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
		this.fullLogs.Add(scrollPresenterId + " ZoomTo requested. Id=" + viewChangeCorrelationId);

		chkLogUpdated.IsChecked = false;

		if (scrollView == this.scrollView52)
			scrollView52ZoomFactorChangeCorrelationId = viewChangeCorrelationId;
	}

	private ScrollView SelectedScrollView
	{
		get
		{
			ScrollView scrollView = null;

			switch (cmbShowScrollView.SelectedIndex)
			{
				case 1:
					scrollView = this.scrollView11;
					break;
				case 2:
					scrollView = this.scrollView21;
					break;
				case 3:
					scrollView = this.scrollView31;
					break;
				case 4:
					scrollView = this.scrollView41;
					break;
				case 5:
					scrollView = this.scrollView51;
					break;
				case 6:
					scrollView = this.scrollView12;
					break;
				case 7:
					scrollView = this.scrollView22;
					break;
				case 8:
					scrollView = this.scrollView32;
					break;
				case 9:
					scrollView = this.scrollView42;
					break;
				case 10:
					scrollView = this.scrollView52;
					break;
			}

			return scrollView;
		}
	}
}
