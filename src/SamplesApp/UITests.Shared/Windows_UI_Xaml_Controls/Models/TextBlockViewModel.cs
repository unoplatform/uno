using System;
using System.Globalization;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class TextBlockViewModel : ViewModelBase
	{
		private string _currentDate;
		private long _increasingSize;
		private string _increasingText;
		private string _alternatingLongText;
		private string _alternatingSmallText;
		private TimeSpan _randomTimeSpan = TimeSpan.FromMinutes(123);

		public TextBlockViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			ObserveAlternatingLongText();
		}

		public string CurrentDate
		{
			get => _currentDate;
			private set
			{
				_currentDate = value;
				RaisePropertyChanged();
			}
		}

		public long IncreasingSize
		{
			get => _increasingSize;
			private set
			{
				_increasingSize = value;
				RaisePropertyChanged();
			}
		}

		public string IncreasingText
		{
			get => _increasingText;
			private set
			{
				_increasingText = value;
				RaisePropertyChanged();
			}
		}

		public string AlternatingLongText
		{
			get => _alternatingLongText;
			private set
			{
				_alternatingLongText = value;
				RaisePropertyChanged();
			}
		}

		public string AlternatingSmallText
		{
			get => _alternatingSmallText;
			private set
			{
				_alternatingSmallText = value;
				RaisePropertyChanged();
			}
		}

		public TimeSpan RandomTimeSpan
		{
			get => _randomTimeSpan;
			private set
			{
				_randomTimeSpan = value;
				RaisePropertyChanged();
			}
		}

		private async void ObserveAlternatingLongText()
		{
			long i = 0;
			while (!CT.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				i++;
				var alternate = i % 2 == 0;

				AlternatingLongText = alternate
					? ""
					: "This is a very long line of text that should wrap properly";

				AlternatingSmallText = alternate
					? ""
					: "Small text";

				IncreasingText = "This is a very long databound text with a number {0}".InvariantCultureFormat((i * 10) % 200);

				IncreasingSize = (i * 10) % 200;

				CurrentDate = DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture);

				RandomTimeSpan += TimeSpan.FromSeconds(20);
			}
		}
	}
}
