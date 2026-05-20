using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace UITests.Shared.Windows_UI_Xaml.xBind
{
	internal class XbindViewModel : ViewModelBase
	{
		private string _stringValue;
		public string StringValue
		{
			get => _stringValue;
			set
			{
				_stringValue = value;
				RaisePropertyChanged(nameof(StringValue));
			}
		}

		private double _dblValue;
		public double DoubleValue
		{
			get => _dblValue;
			set
			{
				_dblValue = value;
				RaisePropertyChanged(nameof(DoubleValue));
			}
		}

		public XbindViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}
	}
}
