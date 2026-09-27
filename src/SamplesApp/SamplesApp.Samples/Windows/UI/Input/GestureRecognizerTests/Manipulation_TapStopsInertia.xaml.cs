using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace UITests.Windows_UI_Input.GestureRecognizerTests
{
	[Sample(
		"Gesture Recognizer",
		Description = "ManipulationExtensions.IsTapToStopInertiaEnabled: the tap which stops the momentum of a ManipulationMode element does not select the pointed row.",
		IsManualTest = true)]
	public sealed partial class Manipulation_TapStopsInertia : Page
	{
		private const int RowCount = 40;
		private const double RowHeight = 45; // Including the 1px margin between the rows.

		private Border _selectedRow;
		private int _selectionCount;

		public Manipulation_TapStopsInertia()
		{
			this.InitializeComponent();

			Rows.ItemsSource = Enumerable.Range(1, RowCount).Select(i => $"Row {i}").ToArray();
			Viewport.SizeChanged += (_, e) => ViewportClip.Rect = new Rect(new Point(), e.NewSize);

			UpdateStatus("ready");
		}

		private void OptIn_Changed(object sender, RoutedEventArgs e)
			=> Viewport.SetIsTapToStopInertiaEnabled(OptIn.IsChecked is true);

		private void Row_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (_selectedRow is { } previous)
			{
				previous.BorderThickness = default;
			}

			var row = (Border)sender;
			row.BorderThickness = new Thickness(3);
			_selectedRow = row;
			_selectionCount++;

			UpdateStatus($"selected {((TextBlock)row.Child).Text}");
		}

		private void Viewport_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
			=> UpdateStatus("coasting");

		private void Viewport_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
			=> UpdateStatus("completed");

		private void Viewport_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var minOffset = Math.Min(0, Viewport.ActualHeight - RowCount * RowHeight);
			RowsTransform.Y = Math.Clamp(RowsTransform.Y + e.Delta.Translation.Y, minOffset, 0);

			if (!e.IsInertial)
			{
				UpdateStatus("dragging");
			}
		}

		private void UpdateStatus(string state)
			=> Status.Text = $"{state} — selections: {_selectionCount}";
	}
}
