using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	internal class FlyoutButonViewModel : ViewModelBase
	{
		public FlyoutButonViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			DataBoundText = "Button not clicked";
		}

		public Command ChangeTextCommand => new Command((p) =>
		{
			DataBoundText = "Button was clicked";
		});

		private string _dataBoundText;

		public string DataBoundText
		{
			get { return _dataBoundText; }
			set
			{
				_dataBoundText = value;
				RaisePropertyChanged();
			}
		}

	}
}
