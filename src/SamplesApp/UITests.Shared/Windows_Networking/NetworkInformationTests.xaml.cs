using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.CustomAttributes;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_Networking
{
	[SampleControlInfo("Windows.Networking", "NetworkInformation", viewModelType: typeof(NetworkInformationViewModel))]
	public sealed partial class NetworkInformationTests : Page
	{
		public NetworkInformationTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += NetworkInformationTests_DataContextChanged;
		}

		private void NetworkInformationTests_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			Model = (NetworkInformationViewModel)args.NewValue;
		}

		public NetworkInformationViewModel Model { get; private set; }
	}

	public class NetworkInformationViewModel : ViewModelBase
	{
		private NetworkConnectivityLevel _networkConnectivityLevel = NetworkConnectivityLevel.None;
		private bool _isObserving = false;
		private string _lastUpdated = "";
		private string _errorInfo = "";
		private NetworkCostType _networkCostType;
		private bool _isWlanConnectionProfile;
		private bool _isWwanConnectionProfile;

		public NetworkInformationViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public NetworkConnectivityLevel NetworkConnectivityLevel
		{
			get => _networkConnectivityLevel;
			set
			{
				_networkConnectivityLevel = value;
				RaisePropertyChanged();
			}
		}

		public string LastUpdated
		{
			get
			{
				return _lastUpdated;
			}
			set
			{
				_lastUpdated = value;
				RaisePropertyChanged();
			}
		}

		public bool IsObserving
		{
			get => _isObserving;
			set
			{
				_isObserving = value;
				RaisePropertyChanged();
			}
		}

		public string ErrorInfo
		{
			get => _errorInfo;
			set
			{
				_errorInfo = value;
				RaisePropertyChanged();
			}
		}

		public NetworkCostType NetworkCostType
		{
			get => _networkCostType;
			private set
			{
				_networkCostType = value;
				RaisePropertyChanged();
			}
		}

		public bool IsWlanConnectionProfile
		{
			get => _isWlanConnectionProfile;
			private set
			{
				_isWlanConnectionProfile = value;
				RaisePropertyChanged();
			}
		}
		public bool IsWwanConnectionProfile
		{
			get => _isWwanConnectionProfile;
			private set
			{
				_isWwanConnectionProfile = value;
				RaisePropertyChanged();
			}
		}

		public void ToggleObserving()
		{
			IsObserving = !IsObserving;
			if (IsObserving)
			{
				NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			}
			else
			{
				NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
			}
		}

		private async void NetworkInformation_NetworkStatusChanged(object sender)
		{
			try
			{
				await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateNetworkInformation());
			}
			catch (Exception ex)
			{
				ErrorInfo = ex.Message;
			}
		}

		public void CheckConnectivity() => UpdateNetworkInformation();

		private void UpdateNetworkInformation()
		{
			LastUpdated = DateTime.UtcNow.ToLongTimeString();
			var profile = NetworkInformation.GetInternetConnectionProfile();
			if (profile != null)
			{
				NetworkConnectivityLevel = profile.GetNetworkConnectivityLevel();
				var connectionCost = profile.GetConnectionCost();
				NetworkCostType = connectionCost.NetworkCostType;
				
				IsWlanConnectionProfile = profile.IsWlanConnectionProfile;
				IsWwanConnectionProfile = profile.IsWwanConnectionProfile;
			}
		}
	}
}
