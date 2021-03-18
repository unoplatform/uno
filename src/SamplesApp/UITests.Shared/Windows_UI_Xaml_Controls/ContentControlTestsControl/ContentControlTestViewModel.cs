using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.UITests.Helpers;
using Uno.UI.Common;
using System.Windows.Input;
using Windows.UI.Core;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	public class ContentControlTestViewModel : ViewModelBase
	{
		public string Property1 { get; set; } = "Property1";
		public string Property2 { get; set; } = "Property2";
		public string Property3 { get; set; } = "Property3";
		public string Property4 { get; set; } = "Property4";

		public ContentControlTestViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SetChangeablePropertyToNull = new DelegateCommand(() => ChangeableProperty = null);
		}

        public TestDataItem SampleData { get; set; } = new TestDataItem()
        {
            UpperString = "This string goes above - databound",
            LowerString = "This string goes below - databound"
        };

		private string _changeableProperty;

		public string ChangeableProperty
		{
			get { return _changeableProperty; }
			set { _changeableProperty = value; RaisePropertyChanged(); }
		}

		public ICommand SetChangeablePropertyToNull { get; } 


		public class TestDataItem
		{
			public string UpperString { get; set; }
			public string LowerString { get; set; }

			public override string ToString()
			{
				return "TestDataItem: " + base.ToString();
			}
		}
	}
}
