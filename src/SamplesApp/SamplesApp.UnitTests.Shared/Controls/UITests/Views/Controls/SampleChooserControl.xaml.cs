using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Input;
using System.Threading;
using SampleControl.Entities;
using Windows.System;
using System.Threading.Tasks;


#if WINAPPSDK
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl : UserControl
	{
		private bool _initialMeasure = true;
		private bool _initialArrange = true;

		public SampleChooserControl()
		{
			this.InitializeComponent();

			// Benchmark hook: UNO_PERF_OPEN_MENU=1 opens the settings (gear) flyout after the scene settles,
			// so the flyout-over-animated-content cost is measurable in scripted runs.
			if (Environment.GetEnvironmentVariable("UNO_PERF_OPEN_MENU") is "1" or "true")
			{
				Loaded += async (_, _) =>
				{
					await Task.Delay(TimeSpan.FromSeconds(30));
					OverflowSettingsButton.Flyout?.ShowAt(OverflowSettingsButton);
					Console.WriteLine("PERF: gear menu opened");
				};
			}

			// Benchmark hook: UNO_PERF_CYCLE=<seconds> walks every sample, dwelling <seconds> on each, with
			// "PERF-NAV:" markers; pairs with the UNO_LOG_FPS hook below for a per-sample FPS sweep.
			if (int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_CYCLE"), out var dwellSeconds) && dwellSeconds > 0)
			{
				Loaded += async (_, _) =>
				{
					if (Environment.GetEnvironmentVariable("UNO_PERF_MAXIMIZE") is "1" or "true"
						&& SamplesApp.App.MainWindow?.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
					{
						presenter.Maximize();
						Console.WriteLine("PERF: window maximized");
					}
					await Task.Delay(TimeSpan.FromSeconds(10));
					await ViewModel.CycleAllSamplesForPerf(dwellSeconds, CancellationToken.None);
				};
			}

			// Benchmark hook: UNO_PERF_SCROLL=1 auto-scrolls the samples list at 60Hz (bounces at the ends),
			// reproducing the realize/derealize churn of manual scrolling without synthesizing input.
			if (Environment.GetEnvironmentVariable("UNO_PERF_SCROLL") is "1" or "true")
			{
				Loaded += async (_, _) =>
				{
					await Task.Delay(TimeSpan.FromSeconds(12));
					var sv = FindTallestScrollViewer(this);
					if (sv is null)
					{
						Console.WriteLine("PERF-SCROLL: no scrollable ScrollViewer found");
						return;
					}
					Console.WriteLine($"PERF-SCROLL: start (scrollable={sv.ScrollableHeight:F0})");
					var dir = 1d;
					var scrollTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
					scrollTimer.Tick += (_, _) =>
					{
						var next = sv.VerticalOffset + dir * 25;
						if (next >= sv.ScrollableHeight) { next = sv.ScrollableHeight; dir = -1; }
						else if (next <= 0) { next = 0; dir = 1; }
						sv.ChangeView(null, next, null, disableAnimation: true);
					};
					scrollTimer.Start();
				};
			}


			if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true")
			{
				var frames = 0;
				var windowStart = DateTime.UtcNow;
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += (_, _) =>
				{
					frames++;
					var elapsed = (DateTime.UtcNow - windowStart).TotalSeconds;
					if (elapsed >= 1)
					{
						Console.WriteLine($"FPS: {frames / elapsed:F1}");
						frames = 0;
						windowStart = DateTime.UtcNow;
					}
				};
			}
		}

		private SampleChooserViewModel ViewModel => (SampleChooserViewModel)DataContext;

		private static ScrollViewer FindTallestScrollViewer(DependencyObject root)
		{
			ScrollViewer best = null;
			var queue = new Queue<DependencyObject>();
			queue.Enqueue(root);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current is ScrollViewer sv && sv.ScrollableHeight > 0 && (best is null || sv.ScrollableHeight > best.ScrollableHeight))
				{
					best = sv;
				}
				var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current);
				for (var i = 0; i < count; i++)
				{
					queue.Enqueue(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i));
				}
			}
			return best;
		}

		private async void FocusSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			// Ensure the pane is open before focusing the search box
			if (!SplitView.IsPaneOpen)
			{
				SplitView.IsPaneOpen = true;
				await Task.Yield();
			}

			SearchBox.Focus(FocusState.Keyboard);
			args.Handled = true;
		}

		private void ReloadSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.ReloadCurrentTestCommand.CanExecute(null))
			{
				ViewModel.ReloadCurrentTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void PreviousSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.LoadPreviousTestCommand.CanExecute(null))
			{
				ViewModel.LoadPreviousTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void NextSampleAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.LoadNextTestCommand.CanExecute(null))
			{
				ViewModel.LoadNextTestCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void FavoritesViewAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			ViewModel.ShowNewSectionCommand.Execute("Favorites");
			args.Handled = true;
		}

		private void HistoryViewAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			ViewModel.ShowNewSectionCommand.Execute("Recents");
			args.Handled = true;
		}

		private void PlaygroundAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.OpenPlaygroundCommand.CanExecute(null))
			{
				ViewModel.OpenPlaygroundCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void RuntimeTestsAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.OpenRuntimeTestsCommand.CanExecute(null))
			{
				ViewModel.OpenRuntimeTestsCommand.Execute(null);
			}
			args.Handled = true;
		}

		private void HelpAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ViewModel is null || !ViewModel.KeyboardShortcutsEnabled)
			{
				return;
			}

			if (ViewModel.OpenHelpCommand.CanExecute(null))
			{
				ViewModel.OpenHelpCommand.Execute(null);
			}
			args.Handled = true;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Assert.IsNotNull(XamlRoot, "XamlRoot was not initialized before measure");
#if HAS_UNO
			Assert.IsTrue(XamlRoot.VisualTree.ContentRoot.CompositionContent.RasterizationScaleInitialized, "Rasterization scale was not initialized");
#endif

			if (_initialMeasure && availableSize == default)
			{
				Assert.Fail("Initial Measure should not be called with empty size");
			}

			_initialMeasure = false;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size availableSize)
		{
			if (_initialArrange && availableSize == default)
			{
				Assert.Fail("Initial Arrange should not be called with empty size");
			}

			_initialArrange = false;
			return base.ArrangeOverride(availableSize);
		}

		private void OnSearchEnterKey_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				((SampleChooserViewModel)DataContext).TryOpenSingleSearchResult();
			}
		}

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				((SampleChooserViewModel)DataContext).SearchTerm = sender.Text;
			}
		}

		private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (args is not null)
			{
				((SampleChooserViewModel)DataContext).TryOpenSingleSearchResult();
			}
		}

		private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is SampleChooserContent control)
			{
				_ = ((SampleChooserViewModel)DataContext).OpenSample(CancellationToken.None, control);
			}
		}

		private void InfoFlyout_Opening(object sender, object e)
		{
			SampleInfoFlyoutContent.DataContext = ViewModel?.CurrentSelectedSample;
		}
	}
}
