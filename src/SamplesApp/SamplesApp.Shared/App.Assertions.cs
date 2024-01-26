using System;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;

#if __SKIA__
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Core;
#endif

namespace SamplesApp;

partial class App
{
	/// <summary>
	/// Assert that ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on all platforms.
	/// </summary>
	/// <seealso href="https://github.com/unoplatform/uno/issues/1741"/>
	public void AssertIssue1790ApplicationSettingsUsable()
	{
		void AssertIsUsable(global::Windows.Storage.ApplicationDataContainer container)
		{
			const string issue1790 = nameof(issue1790);

			container.Values.Remove(issue1790);
			container.Values.Add(issue1790, "ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on this platform.");

			Assert.IsTrue(container.Values.ContainsKey(issue1790));
		}

		AssertIsUsable(global::Windows.Storage.ApplicationData.Current.LocalSettings);
		AssertIsUsable(global::Windows.Storage.ApplicationData.Current.RoamingSettings);
	}

	/// <summary>
	/// Assert that the App DisplayName was found in manifest and loaded from resources
	/// </summary>
	public void AssertIssue12936()
	{
		//On Wasm and XamlIslands the DisplayName is currently empty, as it is not being load from manifest
#if !__WASM__
#if __SKIA__
		if (!CoreApplication.IsFullFledgedApp)
		{
			return;
		}
#endif
		var displayName = Package.Current.DisplayName;

		Assert.IsFalse(string.IsNullOrEmpty(displayName), "DisplayName is empty.");

		Assert.IsFalse(displayName.Contains("ms-resource:"), $"'{displayName}' wasn't found in resources.");
#endif
	}

	/// <summary>
	/// Assert that ApplicationModel Package properties were found in the manifest and loaded from resources 
	/// </summary>
	public void AssertIssue12937()
	{
		//The ApplicationModel Package properties are currently only supported on Skia
#if __SKIA__
		if (!CoreApplication.IsFullFledgedApp)
		{
			return;
		}

		var description = Package.Current.Description;
		var publisherName = Package.Current.PublisherDisplayName;

		Assert.IsFalse(string.IsNullOrEmpty(description), "Description isn't in manifest.");

		Assert.IsFalse(string.IsNullOrEmpty(publisherName), "PublisherDisplayName isn't in manifest.");

		Assert.IsFalse(description.Contains("ms-resource:"), $"'{description}' wasn't found in resources.");

		Assert.IsFalse(publisherName.Contains("ms-resource:"), $"'{publisherName}' wasn't found in resources.");
#endif
	}

	/// <summary>
	/// Assert that Application Title is getting its value from manifest
	/// </summary>
	public void AssertIssue8356()
	{
#if __SKIA__
		Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement_ApplicationView.Given_ApplicationView.StartupTitle = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title;
#endif
	}

	/// <summary>
	/// Assert that the native overlay layer for Skia targets is initialized in time for UI to appear.
	/// </summary>
	public void AssertIssue8641NativeOverlayInitialized()
	{
#if __SKIA__
		// Temporarily add a TextBox to the current page's content to verify native overlay is available
		if (_mainWindow?.Content is not Frame rootFrame)
		{
			throw new InvalidOperationException("Native overlay verification executed too early");
		}

		var textBox = new TextBox();
		var xamlRoot = _mainWindow.RootElement.XamlRoot;
		textBox.XamlRoot = xamlRoot;
		var textBoxView = new TextBoxView(textBox);
		ApiExtensibility.CreateInstance<IOverlayTextBoxViewExtension>(textBoxView, out var textBoxViewExtension);
		Assert.IsNotNull(textBoxViewExtension);

		if (textBoxViewExtension is not null)
		{
			Assert.IsTrue(textBoxViewExtension.IsOverlayLayerInitialized(xamlRoot));
		}
		else
		{
			Console.WriteLine($"TextBoxView is not available for this platform");
		}
#endif
	}

	public void AssertInitialWindowSize()
	{
#if !__SKIA__ // Will be fixed as part of #8341
		Assert.IsTrue(_mainWindow.Bounds.Width > 0);
		Assert.IsTrue(_mainWindow.Bounds.Height > 0);
#endif
	}

	/// <summary>
	/// Verifies that ApplicationData are available immediately after the application class is created
	/// and the data are stored in proper application specific lcoations.
	/// </summary>
	public void AssertApplicationData()
	{
#if __SKIA__
		var appName = Package.Current.Id.Name;
		var publisher = string.IsNullOrEmpty(Package.Current.Id.Publisher) ? "" : "Uno Platform";

		AssertForFolder(ApplicationData.Current.LocalFolder);
		AssertForFolder(ApplicationData.Current.RoamingFolder);
		AssertForFolder(ApplicationData.Current.TemporaryFolder);
		AssertForFolder(ApplicationData.Current.LocalCacheFolder);
		AssertSettings(ApplicationData.Current.LocalSettings);
		AssertSettings(ApplicationData.Current.RoamingSettings);

		void AssertForFolder(StorageFolder folder)
		{
			AssertContainsIdProps(folder);
			AssertCanCreateFile(folder);
		}

		void AssertSettings(ApplicationDataContainer container)
		{
			var key = Guid.NewGuid().ToString();
			var value = Guid.NewGuid().ToString();

			container.Values[key] = value;
			Assert.IsTrue(container.Values.ContainsKey(key));
			Assert.AreEqual(value, container.Values[key]);
			container.Values.Remove(key);
		}

		void AssertContainsIdProps(StorageFolder folder)
		{
			Assert.IsTrue(folder.Path.Contains(appName, StringComparison.Ordinal));
			Assert.IsTrue(folder.Path.Contains(publisher, StringComparison.Ordinal));
		}

		void AssertCanCreateFile(StorageFolder folder)
		{
			var filename = Guid.NewGuid() + ".txt";
			var path = Path.Combine(folder.Path, filename);
			var expectedContent = "Test";
			try
			{
				File.WriteAllText(path, expectedContent);
				var actualContent = File.ReadAllText(path);

				Assert.AreEqual(expectedContent, actualContent);
			}
			finally
			{
				File.Delete(path);
			}
		}
#endif
	}

	private void AssertIssue10313ResumingAfterActivate()
	{
		if (!_wasActivated)
		{
			Assert.Fail("Resuming should never be triggered before initial Window activation.");
		}

		if (!_isSuspended)
		{
			Assert.Fail("Resuming should never be triggered unless the app is suspended.");
		}
	}
}
