using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using Windows.System;
using ICommand = System.Windows.Input.ICommand;

namespace UITests.Shared.Windows_System
{
	[Sample("Windows.System", "Launcher",
		Description: "Tests the Launcher. Some special URIs are supported on certain platforms (Windows, Android, iOS—such as ms-settings:). On Android, the 'Open System Notification Settings' option should take you directly to the 'App Notifications' section in the system settings.",
		ViewModelType: typeof(LauncherTestsViewModel))]
	public sealed partial class LauncherTests : UserControl
	{
		public LauncherTests()
		{
			this.InitializeComponent();
		}
	}

	internal class LauncherTestsViewModel : ViewModelBase
	{
		private string _uri = "https://platform.uno";
		private string _error;
		private LaunchQuerySupportStatus _supportResult;

		public LauncherTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public string Uri
		{
			get => _uri;
			set
			{
				_uri = value;
				RaisePropertyChanged();
			}
		}

		public string Error
		{
			get => _error;
			set
			{
				_error = value;
				RaisePropertyChanged();
			}
		}

		public LaunchQuerySupportStatus SupportResult
		{
			get => _supportResult;
			set
			{
				_supportResult = value;
				RaisePropertyChanged();
			}
		}

		public ICommand QuerySupportCommand => GetOrCreateCommand(QuerySupport);

		public ICommand LaunchCommand => GetOrCreateCommand(() => Launch());

		public ICommand OpenFileCommand => GetOrCreateCommand(OpenFile);

		private async void OpenFile()
		{
			var filePath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Hello.txt");
			File.WriteAllText(filePath, "Hello, world!");
			var file = await StorageFile.GetFileFromPathAsync(filePath);
			await Launcher.LaunchFileAsync(file);
		}

		public ICommand OpenFolderCommand => GetOrCreateCommand(OpenFolder);

		private async void OpenFolder()
		{
			var path = ApplicationData.Current.LocalFolder.Path;
			await Launcher.LaunchFolderPathAsync(path);
		}

		private async void Launch(string uriParam = null)
		{
			Error = "";
			try
			{
				if (System.Uri.TryCreate(uriParam ?? Uri, UriKind.Absolute, out var parsedUri))
				{
					await Launcher.LaunchUriAsync(parsedUri);
				}
				else
				{
					Error = "Can't parse input as an absolute URI.";
				}
			}
			catch (Exception ex)
			{
				Error = ex.ToString();
			}
		}

		private async void QuerySupport()
		{
			Error = "";
			try
			{
				if (System.Uri.TryCreate(Uri, UriKind.Absolute, out var parsedUri))
				{
					SupportResult = await Launcher.QueryUriSupportAsync(parsedUri, LaunchQuerySupportType.Uri);
				}
				else
				{
					Error = "Can't parse input as an absolute URI.";
				}
			}
			catch (Exception ex)
			{
				Error = ex.ToString();
			}
		}

		public ICommand OpenAppNotificationSettingsCommand => GetOrCreateCommand(OpenAppNotificationSettings);

		private void OpenAppNotificationSettings() => Launch("ms-settings:notifications");
	}
}
