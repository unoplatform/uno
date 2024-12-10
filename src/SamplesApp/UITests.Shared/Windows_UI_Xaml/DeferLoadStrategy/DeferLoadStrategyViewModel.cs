using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Private.Infrastructure;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class DeferLoadStrategyViewModel : ViewModelBase
	{
		private Visibility _lateVisibility = Visibility.Collapsed;

		public DeferLoadStrategyViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			var _ = dispatcher.RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				async () =>
				{
					await Task.Delay(3000);
					LateVisibility = Visibility.Visible;
				}
			);
		}

		public Visibility LateVisibility
		{
			get => _lateVisibility;
			private set
			{
				_lateVisibility = value;
				RaisePropertyChanged();
			}
		}
	}
}
