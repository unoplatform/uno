using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Microsoft.UI.Xaml;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class DeferLoadStrategyViewModel : ViewModelBase
	{
		private Visibility _lateVisibility = Visibility.Collapsed;

		public DeferLoadStrategyViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			var _ = dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
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
