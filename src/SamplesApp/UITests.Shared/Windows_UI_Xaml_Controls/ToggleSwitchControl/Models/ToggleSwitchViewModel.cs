using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using System.Windows.Input;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	public class ToggleSwitchViewModel : ViewModelBase
	{
		private bool _isOn = true;

		public ToggleSwitchViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			Flip = CreateCommand(OnFlip);
		}

		private void OnFlip(object obj) => IsOn = !IsOn;

		public bool IsOn
		{
			get => _isOn;
			set
			{
				_isOn = value;
				RaisePropertyChanged();
			}
		}

		public ICommand Flip { get; }
	}
}
