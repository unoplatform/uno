using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Windows_Networking
{
	[Sample("Windows.Networking", "NetworkInformation", ViewModelType: typeof(NetworkInformationViewModel))]
	public sealed partial class NetworkInformationTests : Page
	{
		public NetworkInformationTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += NetworkInformationTests_DataContextChanged;
		}

		private void NetworkInformationTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = (NetworkInformationViewModel)args.NewValue;
		}

		internal NetworkInformationViewModel Model { get; private set; }
	}

	internal class NetworkInformationViewModel : ViewModelBase
	{
		private NetworkConnectivityLevel _networkConnectivityLevel = NetworkConnectivityLevel.None;
		private bool _isObserving = false;
		private string _lastUpdated = "";
		private string _errorInfo = "";
		private string _networkCostType;
		private string _isWlanConnectionProfile;
		private string _isWwanConnectionProfile;

		public NetworkInformationViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public ICommand CheckConnectivityCommand => GetOrCreateCommand(CheckConnectivity);

		public ICommand ToggleObservingCommand => GetOrCreateCommand(ToggleObserving);

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

		public string NetworkCostType
		{
			get => _networkCostType;
			private set
			{
				_networkCostType = value;
				RaisePropertyChanged();
			}
		}

		public string IsWlanConnectionProfile
		{
			get => _isWlanConnectionProfile;
			private set
			{
				_isWlanConnectionProfile = value;
				RaisePropertyChanged();
			}
		}
		public string IsWwanConnectionProfile
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
				await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => UpdateNetworkInformation());
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

				NetworkCostType = GetStringSafe(() =>
				{
					var connectionCost = profile.GetConnectionCost();
					return connectionCost.NetworkCostType.ToString();
				});

				IsWlanConnectionProfile = GetStringSafe(() => profile.IsWlanConnectionProfile.ToString());
				IsWwanConnectionProfile = GetStringSafe(() => profile.IsWwanConnectionProfile.ToString());
			}
		}

		private string GetStringSafe(Func<string> func)
		{
			try
			{
				return func();
			}
			catch (NotImplementedException)
			{
				return "(not implemented)";
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}
	}
}
