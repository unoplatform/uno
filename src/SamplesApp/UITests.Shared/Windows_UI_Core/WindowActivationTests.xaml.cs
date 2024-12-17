using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
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
	[SampleControlInfo("Windows.UI.Core", controlName: "Application/Window lifecycle events", viewModelType: typeof(WindowActivationViewModel), isManualTest: true)]
	public sealed partial class WindowActivationTests : Page
	{
		public WindowActivationTests()
		{
			InitializeComponent();
			DataContextChanged += WindowActivationTests_DataContextChanged;
		}

		internal WindowActivationViewModel Model { get; private set; }

		private void WindowActivationTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = (WindowActivationViewModel)args.NewValue;
		}
	}

	internal class WindowActivationViewModel : ViewModelBase
	{
		private string _changeTime;
		private CoreWindowActivationState? _coreWindowActivationState;
		private CoreWindowActivationMode? _coreWindowActivationMode = Window.Current.CoreWindow.ActivationMode;
		private string _windowVisibility = Window.Current.Visible ? "Visible" : "Hidden";

		public WindowActivationViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			var coreWindow = CoreWindow.GetForCurrentThread();
			if (coreWindow is not null)
			{
				coreWindow.Activated += CoreWindowActivated;
			}
			XamlWindow.Current.Activated += WindowActivated;
			XamlWindow.Current.VisibilityChanged += WindowVisibilityChanged;
#if !WINAPPSDK
			Application.Current.EnteredBackground += AppEnteredBackground;
			Application.Current.LeavingBackground += AppLeavingBackground;
			Application.Current.Suspending += ApplicationSuspending;
			Application.Current.Resuming += ApplicationResuming;
#endif
			CoreApplication.EnteredBackground += CoreApplicationEnteredBackground;
			CoreApplication.LeavingBackground += CoreApplicationLeavingBackground;
			CoreApplication.Suspending += CoreApplicationSuspending;
			CoreApplication.Resuming += CoreApplicationResuming;

			Disposables.Add(() =>
			{
				if (coreWindow is not null)
				{
					coreWindow.Activated -= CoreWindowActivated;
				}
				XamlWindow.Current.Activated -= WindowActivated;
				XamlWindow.Current.VisibilityChanged -= WindowVisibilityChanged;
#if !WINAPPSDK
				Application.Current.EnteredBackground -= AppEnteredBackground;
				Application.Current.LeavingBackground -= AppLeavingBackground;
				Application.Current.Suspending -= ApplicationSuspending;
				Application.Current.Resuming -= ApplicationResuming;
#endif
				CoreApplication.EnteredBackground -= CoreApplicationEnteredBackground;
				CoreApplication.LeavingBackground -= CoreApplicationLeavingBackground;
				CoreApplication.Suspending -= CoreApplicationSuspending;
				CoreApplication.Resuming -= CoreApplicationResuming;
			});
		}

		public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

		public ICommand ClearHistoryCommand => GetOrCreateCommand(() => History.Clear());

		public ICommand CopyToClipboardCommand => GetOrCreateCommand(() => CopyToClipboard());

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

		public bool SimulateDeferrals { get; set; }

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
#if HAS_UNO_WINUI || WINAPPSDK
			Microsoft/* UWP don't rename */.UI.Xaml.WindowActivatedEventArgs e
#else
			Windows.UI.Core.WindowActivatedEventArgs e
#endif
			)
		{
#if !WINAPPSDK
			CoreWindowActivationState = e.WindowActivationState;
#endif
			AddHistory("Window.Activated");
		}

#if WINAPPSDK
		private void WindowVisibilityChanged(object sender, WindowVisibilityChangedEventArgs e)
#else
		private void WindowVisibilityChanged(object sender, VisibilityChangedEventArgs e)
#endif
		{
			WindowVisibility = XamlWindow.Current.Visible ? "Visible" : "Hidden";
			AddHistory("Window.VisibilityChanged");
		}


		private async void AppLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
		{
			AddHistory("Application.LeavingBackground started");
			if (SimulateDeferrals)
			{
				var deferral = e.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("Application.LeavingBackground ended");
		}

		private async void CoreApplicationLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
		{
			AddHistory("CoreApplication.LeavingBackground started");
			if (SimulateDeferrals)
			{
				var deferral = e.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("CoreApplication.LeavingBackground ended");
		}

		private async void AppEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
		{
			AddHistory("Application.EnteredBackground started");
			if (SimulateDeferrals)
			{
				var deferral = e.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("Application.EnteredBackground ended");
		}

		private async void CoreApplicationEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
		{
			AddHistory("CoreApplication.EnteredBackground started");
			if (SimulateDeferrals)
			{
				var deferral = e.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("CoreApplication.EnteredBackground ended");
		}

		private void ApplicationResuming(object sender, object e)
		{
			AddHistory("Application.Resuming");
		}

		private void CoreApplicationResuming(object sender, object e)
		{
			AddHistory("CoreApplication.Resuming");
		}

		private async void CoreApplicationSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			AddHistory("CoreApplication.Suspending started");
			if (SimulateDeferrals)
			{
				var deferral = e.SuspendingOperation.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("CoreApplication.Suspending ended");
		}

		private async void ApplicationSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			AddHistory("Application.Suspending started");
			if (SimulateDeferrals)
			{
				var deferral = e.SuspendingOperation.GetDeferral();
				await Task.Delay(500);
				deferral.Complete();
			}
			AddHistory("Application.Suspending ended");
		}

		private void AddHistory(string eventName)
		{
			ChangeTime = DateTime.Now.ToLongTimeString();
			var historyItem =
				$"{DateTime.Now.ToLongTimeString()} | {eventName} | State: {CoreWindowActivationState} " +
				$"| Mode: {CoreWindow.GetForCurrentThread()?.ActivationMode} | Visibility: {XamlWindow.Current.Visible}";
			History.Insert(0, historyItem);
		}

		private void CopyToClipboard()
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(string.Join(Environment.NewLine, History));
			Clipboard.SetContent(dataPackage);
		}
	}
}
