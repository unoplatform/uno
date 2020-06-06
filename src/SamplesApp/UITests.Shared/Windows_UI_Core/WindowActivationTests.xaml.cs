using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.WebUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Core
{
	[SampleControlInfo("Windows.UI.Core", viewModelType: typeof(WindowActivationViewModel))]
    public sealed partial class WindowActivationTests : Page
    {
        public WindowActivationTests()
        {
            this.InitializeComponent();
			this.DataContextChanged += WindowActivationTests_DataContextChanged;
        }

		public WindowActivationViewModel Model { get; private set; }

		private void WindowActivationTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = (WindowActivationViewModel)args.NewValue;
		}
	}

	public class WindowActivationViewModel : ViewModelBase
	{
		private string _changeTime;
		private CoreWindowActivationState _coreWindowActivationState;
		private CoreWindowActivationMode _coreWindowActivationMode;

		public WindowActivationViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{			
			CoreWindow.GetForCurrentThread().Activated += WindowActivationViewModel_Activated;
			Disposables.Add(() =>
			{
				CoreWindow.GetForCurrentThread().Activated -= WindowActivationViewModel_Activated;
			});
		}

		private void WindowActivationViewModel_Activated(CoreWindow sender, WindowActivatedEventArgs args)
		{
			CoreWindowActivationState = args.WindowActivationState;
			CoreWindowActivationMode = sender.ActivationMode;
			ChangeTime = DateTime.Now.ToLongTimeString();
		}

		public CoreWindowActivationState CoreWindowActivationState
		{
			get => _coreWindowActivationState;
			set
			{
				_coreWindowActivationState = value;
				RaisePropertyChanged();
			}
		}

		public CoreWindowActivationMode CoreWindowActivationMode
		{
			get => _coreWindowActivationMode;
			set
			{
				_coreWindowActivationMode = value;
				RaisePropertyChanged();
			}
		}

		public string ChangeTime
		{
			get => _changeTime;
			set
			{
				_changeTime = value;
				RaisePropertyChanged();
			}
		}
	}
}
