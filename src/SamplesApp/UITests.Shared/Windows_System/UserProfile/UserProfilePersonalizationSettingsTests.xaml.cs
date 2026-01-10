using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_System.UserProfile
{
	[Sample(
		"Windows.System",
		description: "Allows setting user's wallpaper and lockscreen",
		viewModelType: typeof(UserProfilePersonalizationSettingsTestsViewModel))]
	public sealed partial class UserProfilePersonalizationSettingsTests : Page
	{
		public UserProfilePersonalizationSettingsTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += UserProfilePersonalizationSettingsTests_DataContextChanged;
		}

		internal UserProfilePersonalizationSettingsTestsViewModel Model { get; private set; }

		private void UserProfilePersonalizationSettingsTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as UserProfilePersonalizationSettingsTestsViewModel;
		}
	}

	internal class UserProfilePersonalizationSettingsTestsViewModel : ViewModelBase
	{
		private UserProfilePersonalizationSettings _personalizationSettings;

		public UserProfilePersonalizationSettingsTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) :
			base(dispatcher)
		{
			_personalizationSettings = UserProfilePersonalizationSettings.Current;
		}

		public bool IsSupported => UserProfilePersonalizationSettings.IsSupported();

		public ICommand SetWallpaperCommand => GetOrCreateCommand(SetWallpaperAsync);

		public ICommand SetLockScreenCommand => GetOrCreateCommand(SetLockScreenAsync);

		private async void SetWallpaperAsync()
		{
			var file = await GetOrCreateAppDataImageFileAsync("Wallpaper.png");
			var success = await _personalizationSettings.TrySetWallpaperImageAsync(file);
			await ShowResultDialogAsync(success);
		}

		private async void SetLockScreenAsync()
		{
			var file = await GetOrCreateAppDataImageFileAsync("LockScreen.png");
			var success = await _personalizationSettings.TrySetLockScreenImageAsync(file);
			await ShowResultDialogAsync(success);
		}

		private async Task ShowResultDialogAsync(bool result)
		{
			var dialog = new MessageDialog(result ? "Image set successfully." : "Image could not be set.");
			await dialog.ShowAsync();
		}

		private async Task<StorageFile> GetOrCreateAppDataImageFileAsync(string imageName)
		{
			var appDataPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, imageName);
			if (!File.Exists(appDataPath))
			{
				var assembly = GetType().Assembly;
				var embeddedResource = assembly.GetManifestResourceNames().FirstOrDefault(
					r => r.EndsWith(imageName, StringComparison.InvariantCultureIgnoreCase));
				using var stream = assembly.GetManifestResourceStream(embeddedResource);
				using var fileStream = File.Create(appDataPath);
				await stream.CopyToAsync(fileStream);
			}
			return await ApplicationData.Current.LocalFolder.GetFileAsync(imageName);
		}
	}
}
