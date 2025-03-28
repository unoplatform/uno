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
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using RefreshVisualizer = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshVisualizer;
using RefreshRequestedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshRequestedEventArgs;
using RefreshStateChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshStateChangedEventArgs;
using RefreshVisualizerOrientation = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshVisualizerOrientation;
using Uno.UI.Samples.Controls;
#if HAS_UNO
using IRefreshVisualizerPrivate = Microsoft.UI.Private.Controls.IRefreshVisualizerPrivate;
using RefreshVizualizer_TestUI;
#endif

namespace MUXControlsTestApp
{
	[Sample("MUX", "WinUI", "PullToRefresh")]
	public sealed partial class RefreshVisualizerPage : TestPage
	{
		private DispatcherTimer timer = new DispatcherTimer();
		private DispatcherTimer adapterTimer = new DispatcherTimer();
		int BackgroundColorCounter = 0;
		int ForegroundColorCounter = 0;

		SymbolIcon icon = new SymbolIcon(Symbol.Cancel);

		Grid grid = new Grid();

		public RefreshVisualizerPage()
		{
			this.InitializeComponent();
			this.RefreshButton.Click += RefreshButtonClick;
			this.InsertIcon.Click += InsertButtonClick;
			this.InsertGrid.Click += InsertGridButtonClick;
			this.ChangeForeground.Click += ChangeForegroundClick;
			this.ChangeBackground.Click += ChangeBackgroundClick;
			this.MarginSlider.ValueChanged += MarginValueChanged;
			this.MySizeSlider.ValueChanged += SizeValueChanged;
			this.Loaded += MainPageLoaded;

			timer.Interval = new TimeSpan(0, 0, 3);
			timer.Tick += Timer_Tick;

			SymbolIcon icon = new SymbolIcon(Symbol.Cancel);
			icon.Margin = new Thickness(15);
			icon.VerticalAlignment = VerticalAlignment.Center;
			icon.HorizontalAlignment = HorizontalAlignment.Center;
			icon.Height = 30;
			icon.Width = 30;

			grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 0));
			grid.Margin = new Thickness(15);
			grid.VerticalAlignment = VerticalAlignment.Center;
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			grid.Height = 30;
			grid.Width = 30;

			LogController.InitializeLogging();
		}

		protected
#if HAS_UNO
			internal
#endif
			override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			timer.Stop();
			adapterTimer.Stop();
		}

		private void InsertGridButtonClick(object sender, RoutedEventArgs e)
		{
			this.RefreshVisualizer.Content = grid;
		}

		private void MarginValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			icon.Margin = new Thickness(e.NewValue);
		}
		private void SizeValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			icon.Height = e.NewValue;
			icon.Width = e.NewValue;
		}

		private void ChangeBackgroundClick(object sender, RoutedEventArgs e)
		{
			switch (BackgroundColorCounter)
			{
				case 0:
					this.RefreshVisualizer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
					break;
				case 1:
					this.RefreshVisualizer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 0));
					break;
				case 2:
					this.RefreshVisualizer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255));
					break;
				default:
					this.RefreshVisualizer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
					BackgroundColorCounter = -1;
					break;
			}
			BackgroundColorCounter++;
		}

		private void ChangeForegroundClick(object sender, RoutedEventArgs e)
		{
			switch (ForegroundColorCounter)
			{
				case 0:
					this.RefreshVisualizer.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
					break;
				case 1:
					this.RefreshVisualizer.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 0));
					break;
				case 2:
					this.RefreshVisualizer.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255));
					break;
				default:
					this.RefreshVisualizer.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
					ForegroundColorCounter = -1;
					break;
			}
			ForegroundColorCounter++;
		}

		private void InsertButtonClick(object sender, RoutedEventArgs e)
		{
			this.RefreshVisualizer.Content = icon;
		}

		private void RefreshButtonClick(object sender, RoutedEventArgs e)
		{
			this.RefreshVisualizer.RequestRefresh();
		}

		private void MainPageLoaded(object sender, object e)
		{
			this.Loaded -= MainPageLoaded;
			this.RefreshVisualizer.RefreshRequested += RefreshVisualizer_RefreshRequested;
#if HAS_UNO
			var adapter = new SliderRefreshInfoProviderAdapter(this.Slider, adapterTimer);
			((IRefreshVisualizerPrivate)this.RefreshVisualizer).InfoProvider = adapter.adapt();
#endif
			this.OrientationComboBox.Items.Add(RefreshVisualizerOrientation.Auto);
			this.OrientationComboBox.Items.Add(RefreshVisualizerOrientation.Normal);
			this.OrientationComboBox.Items.Add(RefreshVisualizerOrientation.Rotate90DegreesCounterclockwise);
			this.OrientationComboBox.Items.Add(RefreshVisualizerOrientation.Rotate270DegreesCounterclockwise);
			this.RefreshVisualizer.RefreshStateChanged += RefreshVisualizer_RefreshStateChanged;
			this.StateText.Text = this.RefreshVisualizer.State.ToString();
			this.OrientationComboBox.SelectedIndex = (int)this.RefreshVisualizer.Orientation;
			this.OrientationComboBox.SelectionChanged += OrientationComboBox_SelectionChanged;
			this.RefreshVisualizer.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 0));
		}

		private void RefreshVisualizer_RefreshStateChanged(RefreshVisualizer sender, RefreshStateChangedEventArgs args)
		{
			this.StateText.Text = args.NewState.ToString();
		}

		private void OrientationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.RefreshVisualizer.Orientation = (RefreshVisualizerOrientation)this.OrientationComboBox.SelectedItem;
		}

		private void RefreshVisualizer_RefreshRequested(object sender, RefreshRequestedEventArgs e)
		{
			this.RefreshCompletionDeferral = e.GetDeferral();
			timer.Start();
		}

		private Deferral RefreshCompletionDeferral
		{
			get;
			set;
		}

		private void Timer_Tick(object sender, object e)
		{
			timer.Stop();
			this.RefreshCompletionDeferral.Complete();
		}
	}
}
