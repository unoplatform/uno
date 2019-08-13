using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
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

namespace UITests.Shared.Windows_UI_Xaml.BindableDrawerLayout
{
	[SampleControlInfoAttribute("BindableDrawerLayout", "BindableDrawerLayout_LeftRightPaneEnabled", typeof(BindableDrawerLayoutViewModel))]
	public sealed partial class BindableDrawerLayout_LeftRightPaneEnabled : UserControl
	{
		public BindableDrawerLayout_LeftRightPaneEnabled()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	public class BindableDrawerLayoutViewModel : ViewModelBase
	{
		private string _leftPaneEnabled;
		private string _rightPaneEnabled;
		private string _enabled;

		public BindableDrawerLayoutViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_leftPaneEnabled = true;
			_rightPaneEnabled = true;
			_enabled = true;
		}

		public double LeftPaneEnabled
		{
			get => _leftPaneEnabled;
			set
			{
				_leftPaneEnabled = value;
				RaisePropertyChanged();
			}
		}

		public double RightPaneEnabled
		{
			get => _rightPaneEnabled;
			set
			{
				_rightPaneEnabled = value;
				RaisePropertyChanged();
			}
		}

		public double Enabled
		{
			get => _enabled;
			set
			{
				_enabled = value;
				RaisePropertyChanged();
			}
		}
	}
}
