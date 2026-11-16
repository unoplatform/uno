using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Slider
{
	internal class SliderViewModel : ViewModelBase
	{
		private double _sliderValue;

		public SliderViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public double SliderValue
		{
			get => _sliderValue;
			set
			{
				_sliderValue = value;
				RaisePropertyChanged();
			}
		}
	}
}
