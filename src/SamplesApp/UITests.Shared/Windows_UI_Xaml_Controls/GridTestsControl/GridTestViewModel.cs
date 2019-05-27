using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml_Controls.GridTestsControl
{
	public class GridTestsViewModel : ViewModelBase
	{
		public GridTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			ObserveGridColumnAlternating();
		}

		public long GridColumnAlternating { get; set; }

		public object GridWidthAlternatingNull { get; set; }

		private async void ObserveGridColumnAlternating()
		{
			long i = 0;
			while (!CT.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				i++;
				GridColumnAlternating = i % 2;
				GridWidthAlternatingNull = (i % 2) == 0 ? null : (object)50;
			}
		}
	}
}
