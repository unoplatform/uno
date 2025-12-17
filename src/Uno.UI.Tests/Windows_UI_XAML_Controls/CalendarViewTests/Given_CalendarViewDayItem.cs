using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;

namespace Uno.UI.Tests.CalendarViewTests
{
	[TestClass]
	public class Given_CalendarViewDayItem
	{
		[TestMethod]
		public void When_SetDensityColors_WithNull_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// Should not throw exception
			dayItem.SetDensityColors(null);
		}

		[TestMethod]
		public void When_SetDensityColors_WithEmptyList_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			var colors = new List<Color>();
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithSingleColor_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			var colors = new List<Color> { Colors.Red };
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithMultipleColors_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			var colors = new List<Color>
			{
				Colors.Red,
				Colors.Green,
				Colors.Blue,
				Colors.Yellow
			};
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithMaxColors_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// Maximum of 10 density bars as per WinUI specification
			var colors = new List<Color>();
			for (int i = 0; i < 10; i++)
			{
				colors.Add(Colors.Blue);
			}
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithMoreThanMaxColors_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// More than 10 colors - only first 10 should be used
			var colors = new List<Color>();
			for (int i = 0; i < 15; i++)
			{
				colors.Add(Colors.Red);
			}
			
			// Should not throw exception (extra colors should be ignored)
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_CalledMultipleTimes_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// First call
			var colors1 = new List<Color> { Colors.Red, Colors.Green };
			dayItem.SetDensityColors(colors1);
			
			// Second call - should replace previous colors
			var colors2 = new List<Color> { Colors.Blue, Colors.Yellow, Colors.Orange };
			dayItem.SetDensityColors(colors2);
			
			// Third call with null - should clear colors
			dayItem.SetDensityColors(null);
			
			// Fourth call with new colors
			var colors3 = new List<Color> { Colors.Purple };
			dayItem.SetDensityColors(colors3);
		}

		[TestMethod]
		public void When_SetDensityColors_WithIEnumerable_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// Test with IEnumerable that's not a list
			IEnumerable<Color> colors = Enumerable.Range(0, 5).Select(i => Colors.Blue);
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithArray_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// Test with array
			var colors = new[] { Colors.Red, Colors.Green, Colors.Blue };
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}

		[TestMethod]
		public void When_SetDensityColors_WithTransparentColors_ShouldNotThrow()
		{
			var dayItem = new CalendarViewDayItem();
			
			// Test with transparent colors
			var colors = new List<Color>
			{
				Color.FromArgb(0, 255, 0, 0), // Transparent red
				Color.FromArgb(128, 0, 255, 0), // Semi-transparent green
				Colors.Blue
			};
			
			// Should not throw exception
			dayItem.SetDensityColors(colors);
		}
	}
}
