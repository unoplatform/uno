using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml_Controls.GridTestsControl
{
	internal class GridTestsViewModel : ViewModelBase
	{
		private long _gridColumnAlternating;
		private object _gridWidthAlternatingNull;

		public GridTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			ObserveGridColumnAlternating();
		}

		public long GridColumnAlternating
		{
			get => _gridColumnAlternating;
			set
			{
				_gridColumnAlternating = value;
				RaisePropertyChanged();
			}
		}

		public object GridWidthAlternatingNull
		{
			get => _gridWidthAlternatingNull;
			set
			{
				_gridWidthAlternatingNull = value;
				RaisePropertyChanged();
			}
		}

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
