using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml_Controls.CalendarView
{
	[Sample("Pickers", Description = "Demonstrates the SetDensityColors method for CalendarView", IgnoreInSnapshotTests = true)]
	public sealed partial class CalendarView_DensityColors : Page
	{
		private enum DensityMode
		{
			None,
			Fixed,
			Random
		}

		private DensityMode _currentMode = DensityMode.None;
		private readonly Random _random = new Random();

		public CalendarView_DensityColors()
		{
			this.InitializeComponent();
		}

		private void AddDensity_Click(object sender, RoutedEventArgs e)
		{
			_currentMode = DensityMode.Fixed;
			InfoText.Text = "Showing fixed density bars (3 bars: Red, Orange, Yellow) on visible days.";
			
			// Force re-rendering of day items
			sut.SetDisplayDate(DateTimeOffset.Now);
		}

		private void ClearDensity_Click(object sender, RoutedEventArgs e)
		{
			_currentMode = DensityMode.None;
			InfoText.Text = "Cleared all density bars.";
			
			// Force re-rendering of day items
			sut.SetDisplayDate(DateTimeOffset.Now);
		}

		private void RandomDensity_Click(object sender, RoutedEventArgs e)
		{
			_currentMode = DensityMode.Random;
			InfoText.Text = "Showing random density bars (1-5 bars with random colors) on visible days.";
			
			// Force re-rendering of day items
			sut.SetDisplayDate(DateTimeOffset.Now);
		}

		private void OnCalendarViewDayItemChanging(Microsoft.UI.Xaml.Controls.CalendarView sender, CalendarViewDayItemChangingEventArgs args)
		{
			// Apply density colors based on current mode
			switch (_currentMode)
			{
				case DensityMode.Fixed:
					SetFixedDensityColors(args.Item);
					break;
				
				case DensityMode.Random:
					SetRandomDensityColors(args.Item);
					break;
				
				case DensityMode.None:
				default:
					args.Item.SetDensityColors(null);
					break;
			}
		}

		private void SetFixedDensityColors(CalendarViewDayItem dayItem)
		{
			// Show 3 density bars with fixed colors
			var colors = new List<Color>
			{
				Colors.Red,
				Colors.Orange,
				Colors.Yellow
			};
			
			dayItem.SetDensityColors(colors);
		}

		private void SetRandomDensityColors(CalendarViewDayItem dayItem)
		{
			// Show 1-5 random density bars
			var colorCount = _random.Next(1, 6);
			var colors = new List<Color>();

			var availableColors = new[]
			{
				Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green,
				Colors.Blue, Colors.Purple, Colors.Pink, Colors.Cyan,
				Colors.Magenta, Colors.Lime
			};

			for (int i = 0; i < colorCount; i++)
			{
				colors.Add(availableColors[_random.Next(availableColors.Length)]);
			}

			dayItem.SetDensityColors(colors);
		}
	}
}
