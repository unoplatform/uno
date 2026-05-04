using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
#if !WINAPPSDK
using _UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode;
#endif

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", Name = "ScrollViewer_UpdatesMode")]
	public sealed partial class ScrollViewer_UpdatesMode : Page
	{
		private List<(bool isIntermediate, CoreDispatcherPriority priority)> ViewChangesOutput { get; } = new List<(bool isIntermediate, CoreDispatcherPriority priority)>();

		public ScrollViewer_UpdatesMode()
		{
			this.InitializeComponent();

#if WINAPPSDK
		}
#else
			_modes.ItemsSource = Enum.GetNames<_UpdatesMode>();
			_modes.SelectedIndex = 0;
			_modes.SelectionChanged += OnModesOnSelectionChanged;
			SetMode(Enum.GetValues<_UpdatesMode>()[0]);

			_scroll.ViewChanged += (snd, e) =>
			{
				ViewChangesOutput.Add((e.IsIntermediate, Dispatcher.CurrentPriority));
				_output.Text += $"intermediate: {e.IsIntermediate} | priority: {Dispatcher.CurrentPriority}\r\n";
			};
		}

		public void SetMode(_UpdatesMode mode)
		{
			Uno.UI.Xaml.Controls.ScrollViewer.SetUpdatesMode(_scroll, mode);
			_modes.SelectedItem = mode.ToString();
			Reset(null, null);
		}

		private void OnModesOnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var modeStr = (string)e.AddedItems[0];
			var mode = Enum.Parse<_UpdatesMode>(modeStr);

			SetMode(mode);
		}

		private void SetSync(object sender, TappedRoutedEventArgs e) => SetMode(_UpdatesMode.Synchronous);

		private void SetAsync(object sender, TappedRoutedEventArgs e) => SetMode(_UpdatesMode.AsynchronousIdle);

		private void Reset(object sender, TappedRoutedEventArgs e)
		{
			_result.Text = "** no result **";
			_output.Text = "";
			ViewChangesOutput.Clear();
		}

		private void Validate(object sender, TappedRoutedEventArgs e)
		{
			if (ViewChangesOutput.Count == 0)
			{
				_result.Text = "FAILED";
			}

			var intermediatePriority = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(_scroll) == _UpdatesMode.AsynchronousIdle
				? CoreDispatcherPriority.Idle
				: CoreDispatcherPriority.Normal;
			for (var i = 0; i < ViewChangesOutput.Count - 1; i++)
			{
				var intermediate = ViewChangesOutput[i];
				if (!intermediate.isIntermediate || intermediate.priority != intermediatePriority)
				{
					_result.Text = "FAILED";
					return;
				}
			}

			var final = ViewChangesOutput.Last();
			if (final.isIntermediate || final.priority != CoreDispatcherPriority.Normal)
			{
				_result.Text = "FAILED";
				return;
			}

			_result.Text = "SUCCESS";
		}
#endif
	}
}
