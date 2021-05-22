using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
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
using XamlWindow = Windows.UI.Xaml.Window;

namespace UITests.Windows_UI_Core
{
	[SampleControlInfo("Windows.UI.Core", viewModelType: typeof(WindowActivationViewModel))]
	public sealed partial class WindowActivationTests : Page
	{
		public WindowActivationTests()
		{
			InitializeComponent();
			DataContextChanged += WindowActivationTests_DataContextChanged;
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
		private CoreWindowActivationState? _coreWindowActivationState;
		private CoreWindowActivationMode? _coreWindowActivationMode = Window.Current.CoreWindow.ActivationMode;
		private string _windowVisibility = Window.Current.Visible ? "Visible" : "Hidden";

		public WindowActivationViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			CoreWindow.GetForCurrentThread().Activated += CoreWindowActivated;
			XamlWindow.Current.Activated += WindowActivated;
			XamlWindow.Current.VisibilityChanged += WindowVisibilityChanged;
			Application.Current.EnteredBackground += AppEnteredBackground;
			Application.Current.LeavingBackground += AppLeavingBackground;

			Disposables.Add(() =>
			{
				CoreWindow.GetForCurrentThread().Activated -= CoreWindowActivated;
				XamlWindow.Current.Activated -= WindowActivated;
				XamlWindow.Current.VisibilityChanged -= WindowVisibilityChanged;
				Application.Current.EnteredBackground -= AppEnteredBackground;
				Application.Current.LeavingBackground -= AppLeavingBackground;
			});
		}

		public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

		public ICommand ClearHistoryCommand => GetOrCreateCommand(() => History.Clear());

		public CoreWindowActivationState? CoreWindowActivationState
		{
			get => _coreWindowActivationState;
			set
			{
				_coreWindowActivationState = value;
				RaisePropertyChanged();
			}
		}

		public CoreWindowActivationMode? CoreWindowActivationMode
		{
			get => _coreWindowActivationMode;
			set
			{
				_coreWindowActivationMode = value;
				RaisePropertyChanged();
			}
		}

		public string WindowVisibility
		{
			get => _windowVisibility;
			set
			{
				_windowVisibility = value;
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

		private void CoreWindowActivated(CoreWindow sender, Windows.UI.Core.WindowActivatedEventArgs args)
		{
			CoreWindowActivationState = args.WindowActivationState;
			CoreWindowActivationMode = sender.ActivationMode;
			AddHistory("CoreWindow.Activated");
		}

		private void WindowActivated(object sender,
#if HAS_UNO_WINUI
			Microsoft.UI.Xaml.WindowActivatedEventArgs e
#else
			Windows.UI.Core.WindowActivatedEventArgs e
#endif
			)
		{
			CoreWindowActivationState = e.WindowActivationState;
			AddHistory("Window.Activated");
		}

		private void WindowVisibilityChanged(object sender, VisibilityChangedEventArgs e)
		{
			WindowVisibility = XamlWindow.Current.Visible ? "Visible" : "Hidden";
			AddHistory("Window.VisibilityChanged");
		}


		private void AppLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
		{
			AddHistory("Application.LeavingBackground");
		}

		private void AppEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
		{
			AddHistory("Application.EnteredBackground");
		}

		private void AddHistory(string eventName)
		{
			ChangeTime = DateTime.Now.ToLongTimeString();
			var historyItem =
				$"{DateTime.Now.ToLongTimeString()} | {eventName} | State: {CoreWindowActivationState} " +
				$"| Mode: {CoreWindow.GetForCurrentThread().ActivationMode} | Visibility: {XamlWindow.Current.Visible}";
			History.Insert(0, historyItem);
		}
	}
}
