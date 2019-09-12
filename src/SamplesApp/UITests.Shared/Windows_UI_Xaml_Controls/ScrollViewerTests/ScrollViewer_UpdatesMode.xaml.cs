using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using _UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo("ScrollViewer", "ScrollViewer_UpdatesMode")]
	public sealed partial class ScrollViewer_UpdatesMode : Page
	{
		public List<(bool isIntermediate, CoreDispatcherPriority priority)> ViewChangesOutput { get; } = new List<(bool isIntermediate, CoreDispatcherPriority priority)>();

		public ScrollViewer_UpdatesMode()
		{
			this.InitializeComponent();

			_modes.ItemsSource = Enum.GetNames(typeof(_UpdatesMode));
			_modes.SelectedIndex = 0;
			_modes.SelectionChanged += OnModesOnSelectionChanged;
			SetMode((_UpdatesMode)Enum.GetValues(typeof(_UpdatesMode)).GetValue(0));

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
			_output.Text = "";
		}

		private void OnModesOnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var modeStr = (string)e.AddedItems[0];
			var mode = (_UpdatesMode)Enum.Parse(typeof(_UpdatesMode), modeStr);

			SetMode(mode);
		}

		private void SetSync(object sender, TappedRoutedEventArgs e) => SetMode(_UpdatesMode.Synchronous);

		private void SetAsync(object sender, TappedRoutedEventArgs e) => SetMode(_UpdatesMode.AsynchronousIdle);
	}
}
