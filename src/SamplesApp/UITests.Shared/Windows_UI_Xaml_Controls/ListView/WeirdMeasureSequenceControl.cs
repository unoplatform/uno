using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	public partial class WeirdMeasureSequenceControl : ContentControl
	{
		private bool _firstProblematicArrange = false;

		public event Action MakingProblems;

		public WeirdMeasureSequenceControl()
		{
			Loaded += WeirdMeasureSequenceControl_Loaded;
		}

		private async void WeirdMeasureSequenceControl_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Delay(50);
			_firstProblematicArrange = true;
			InvalidateMeasure();
			MakingProblems?.Invoke();
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = base.ArrangeOverride(finalSize);
			if (_firstProblematicArrange)
			{
				_firstProblematicArrange = false;

				// Mess with ListView's head
				var weirdSize = new Size(finalSize.Width - 30d, finalSize.Height);
				(Content as FrameworkElement)?.Measure(weirdSize);
			}

			return size;
		}
	}
}
