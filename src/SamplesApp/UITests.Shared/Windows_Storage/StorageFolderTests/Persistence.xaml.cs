using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_Storage.StorageFolderTests
{
	[SampleControlInfo("Windows.Storage", "StorageFolder_Persistence", IsManualTest = true,
		Description = "This test is suited for WASM, should show that files in app private folders are correctly persisted and can be edited.")]
	public sealed partial class Persistence : Page
	{
		public Persistence()
		{
			this.InitializeComponent();
		}

		private async void TryLoad(object sender, TappedRoutedEventArgs e)
		{
			var data = ApplicationData.Current;
			var fileContents = await Task.WhenAll(new[]
				{
					data.RoamingFolder,
					data.LocalFolder,
					data.SharedLocalFolder,
					data.TemporaryFolder,
					data.LocalCacheFolder
				}
				.Select(SafeReadTestFile));

			_output.Text = string.Join("\r\n\r\n", fileContents);
		}

		private async void Write(object sender, TappedRoutedEventArgs e)
		{
			var text = $"Append data on {DateTimeOffset.Now:F}";
			var data = ApplicationData.Current;
			await Task.WhenAll(new[]
				{
					data.RoamingFolder,
					data.LocalFolder,
					data.SharedLocalFolder,
					data.TemporaryFolder,
					data.LocalCacheFolder
				}
				.Select(f => SafeAppendToTestFile(f, text)));

			TryLoad(null, null);
		}

		private async Task<string> SafeReadTestFile(StorageFolder folder)
		{
			try
			{
				using (var stream = await folder.OpenStreamForReadAsync("uno-samples-persistence.txt"))
				using (var reader = new StreamReader(stream))
				{
					return $"{folder.Name}:\r\n{new string('*', folder.Name.Length + 1)}\r\n{reader.ReadToEnd()}";
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to read: " + e);

				return $"{folder.Name}:\r\n{new string('*', folder.Name.Length + 1)}\r\nFailed to read content of test file ({folder.Path}): {e.GetType().Name} {e.Message}";
			}
		}

		private async Task SafeAppendToTestFile(StorageFolder folder, string text)
		{
			try
			{
				using (var stream = await folder.OpenStreamForWriteAsync("uno-samples-persistence.txt", CreationCollisionOption.OpenIfExists))
				{
					stream.Seek(0, SeekOrigin.End);
					using (var writer = new StreamWriter(stream) { AutoFlush = true })
					{
						await writer.WriteLineAsync(text);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to write: " + e);
			}
		}
	}
}
