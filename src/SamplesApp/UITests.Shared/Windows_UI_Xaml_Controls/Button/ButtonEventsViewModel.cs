using System;
using System.Collections.Generic;
using System.Text;

using UITests.Shared.Helpers;

using Windows.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	public class ButtonEventsViewModel
	{
		public ButtonEventsViewModel(Windows.UI.Xaml.Controls.Button testBtn, ButtonEventsDataViewModel dataViewModel)
		{
			TestBtn = testBtn;
			DataViewModel = dataViewModel;
		}
		public Windows.UI.Xaml.Controls.Button TestBtn { get; }
		public ButtonEventsDataViewModel DataViewModel { get; }
	}

	public class ButtonEventsDataViewModel : BindableBase
	{
		public ButtonEventsDataViewModel(string testTitle, string expectedBehavior, DispatcherTimer dispatcherTimer = null, Windows.UI.Xaml.Controls.Button testBtn = null)
		{
			TestTitle = testTitle;
			ExpectedBehavior = expectedBehavior;
			Count = _countValue.ToString();
			_dispatcherTimer = dispatcherTimer;
			_testBtn.SetTarget(testBtn);
		}

		private readonly System.WeakReference<Windows.UI.Xaml.Controls.Button> _testBtn = new System.WeakReference<Windows.UI.Xaml.Controls.Button>(null);

		/// <summary>
		/// Stops the DispatcherTimer (if any) and increments Count
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void IncrementCount(object sender, object args)
		{
			_dispatcherTimer?.Stop();
			Count = (++_countValue).ToString();
		}

		private int _countValue;
		private string _count;
		public string Count
		{
			get => _count;
			set
			{
				if (_count != value)
				{
					_count = value;
					RaisePropertyChanged();
				}
			}
		}

		public void ResetCount()
		{
			_countValue = 0;
			Count = _countValue.ToString();
		}

		public string TestTitle { get; }
		public string ExpectedBehavior { get; }

		private DispatcherTimer _dispatcherTimer;

		public void StartDispatcherTimer(object sender, object args)
		{
			_dispatcherTimer?.Start();
		}

		public void StopDispatcherTimer(object sender, object args)
		{
			_dispatcherTimer?.Stop();
		}

		public void TimerTick(object sender, object args)
		{
			if (_testBtn.TryGetTarget(out Windows.UI.Xaml.Controls.Button testBtn))
			{
				testBtn.ReleasePointerCaptures();
			}
		}
	}
}
