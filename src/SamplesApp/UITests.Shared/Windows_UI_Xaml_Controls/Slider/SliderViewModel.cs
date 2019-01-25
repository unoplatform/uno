using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Slider
{
	internal class SliderViewModel : ViewModelBase
	{
		private int _sliderValue;

		public SliderViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public int SliderValue
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
