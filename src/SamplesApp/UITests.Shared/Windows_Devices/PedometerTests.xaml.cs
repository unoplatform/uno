using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo(
		"Windows.Devices",
		"Pedometer",
		description: "Demonstrates the Windows.Devices.Sensors.Pedometer",
		viewModelType: typeof(PedometerTestsViewModel))]
    public sealed partial class PedometerTests : UserControl
    {
        public PedometerTests()
        {
            this.InitializeComponent();
        }
    }

	public class PedometerTestsViewModel : ViewModelBase
	{
		private Pedometer _pedometer = null;
		private string _pedometerStatus;

		public PedometerTestsViewModel(CoreDispatcher dispatcher) :
			base(dispatcher)
		{
		}

		public ICommand GetPedometerCommand => GetOrCreateCommand(GetPedometerAsync);
		
		public string PedometerStatus
		{
			get => _pedometerStatus;
			private set
			{
				_pedometerStatus = value;
				RaisePropertyChanged();
			}
		}

		public bool IsAvailable => _pedometer != null;

		private async void GetPedometerAsync()
		{
			_pedometer = await Pedometer.GetDefaultAsync();
			RaisePropertyChanged(nameof(IsAvailable));
		}
	}
}
