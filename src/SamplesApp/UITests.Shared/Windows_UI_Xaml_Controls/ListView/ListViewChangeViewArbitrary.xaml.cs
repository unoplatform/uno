using System.Collections.Generic;
using System.Text;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListViewChangeViewArbitrary", typeof(ListViewViewModel), description: "ListView sample allowing ChangeView to be called with any value and animations enabled or disabled")]
	public sealed partial class ListViewChangeViewArbitrary : UserControl
	{
		private readonly Queue<ScrollState> _scrollStates = new Queue<ScrollState>();
		private const int MaximumScrollStates = 20;
		public ListViewChangeViewArbitrary()
		{
			this.InitializeComponent();

			Loaded += ListViewChangeViewArbitrary_Loaded;
		}

		private void ListViewChangeViewArbitrary_Loaded(object sender, RoutedEventArgs e)
		{
			//Workaround for bug on Android that TargetListView's template is not materialized yet when Loaded gets called
			TargetListView.ApplyTemplate();

			var scrollViewer = TargetListView.FindFirstChild<Windows.UI.Xaml.Controls.ScrollViewer>();
			scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
		}

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			_scrollStates.Enqueue(new ScrollState { VerticalOffset = (sender as Windows.UI.Xaml.Controls.ScrollViewer).VerticalOffset, IsIntermediate = e.IsIntermediate });
			if (_scrollStates.Count > MaximumScrollStates)
			{
				_scrollStates.Dequeue();
			}

			var sb = new StringBuilder();
			foreach (var state in _scrollStates)
			{
				sb.Append(state);
				sb.Append(',');
			}
			ScrollArgsTextBlock.Text = sb.ToString();
		}

		private void ApplyOffsetDefault(object sender, RoutedEventArgs e)
		{
			var scrollViewer = TargetListView.FindFirstChild<Windows.UI.Xaml.Controls.ScrollViewer>();
			float offset;
			if (float.TryParse(OffsetTextBox.Text, out offset))
			{
				scrollViewer.ChangeView(null, offset, null);
			}
		}

		private void ApplyOffsetChooseAnimate(object sender, RoutedEventArgs e)
		{
			var scrollViewer = TargetListView.FindFirstChild<Windows.UI.Xaml.Controls.ScrollViewer>();
			float offset;
			if (float.TryParse(OffsetTextBox.Text, out offset))
			{
				var disableAnimation = DisableAnimationCheckBox.IsChecked;
				scrollViewer.ChangeView(null, offset, null, disableAnimation ?? false);
			}
		}

		private void ClearScrollArgsText(object sender, RoutedEventArgs e)
		{
			ScrollArgsTextBlock.Text = "(,)";
			_scrollStates.Clear();
		}

		private class ScrollState
		{
			public bool IsIntermediate { get; set; }
			public double VerticalOffset { get; set; }

			public override string ToString()
			{
				return $"({VerticalOffset.ToString("#.##")},{IsIntermediate})";
			}
		}
	}
}
