using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Uno;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Storage.Pickers
{
	[Sample("Windows.Storage", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description =
			"Streams large generated payloads (up to 4 GB) through StorageFile.OpenStreamForWriteAsync in 2 MB chunks. " +
			"On WebAssembly the write path must not buffer the whole file in tab memory: watch the tab's memory in " +
			"DevTools while writing - it should stay roughly flat relative to the file size, and multi-GB saves via " +
			"the save picker must complete without crashing the runtime.")]
	public sealed partial class FileSavePicker_LargeFileWrite : Page
	{
		// 2 MB - matches the chunked-download scenario that surfaced the WASM memory issue.
		private const int ChunkSize = 2_000_000;

		private bool _busy;

		public FileSavePicker_LargeFileWrite()
		{
			this.InitializeComponent();
			this.Loaded += (_, _) => UpdatePickerModeText();
			this.Unloaded += (_, _) =>
			{
#if __CROSSRUNTIME__
				WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = WasmPickerConfiguration.FileSystemAccessApiWithFallback;
#endif
			};
		}

		private void OnPickerModeChanged(object sender, RoutedEventArgs e)
		{
#if __CROSSRUNTIME__
			// The configuration is a global that other picker samples also set, so make
			// this sample's mode explicit rather than inheriting whatever ran before.
			WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = ForceDownloadFallback.IsChecked == true
				? WasmPickerConfiguration.DownloadUpload
				: WasmPickerConfiguration.FileSystemAccessApiWithFallback;
#endif
			UpdatePickerModeText();
		}

		private void UpdatePickerModeText()
		{
#if __CROSSRUNTIME__
			if (OperatingSystem.IsBrowser())
			{
				var configuration = WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration;
				PickerModeText.Text = $"picker mode: {configuration} - saves use " +
					(configuration.HasFlag(WasmPickerConfiguration.FileSystemAccessApi)
						? "the native save dialog where the browser supports it, otherwise a browser download"
						: "a browser download");
				return;
			}
#endif
			PickerModeText.Text = "picker mode: platform picker (browser-specific modes do not apply)";
		}

		private async void OnPicker512(object sender, RoutedEventArgs e) => await SaveViaPickerAsync(512);
		private async void OnPicker1024(object sender, RoutedEventArgs e) => await SaveViaPickerAsync(1024);
		private async void OnPicker2048(object sender, RoutedEventArgs e) => await SaveViaPickerAsync(2048);
		private async void OnPicker4096(object sender, RoutedEventArgs e) => await SaveViaPickerAsync(4096);

		private async void OnLocal128(object sender, RoutedEventArgs e) => await SaveToAppStorageAsync(128);
		private async void OnLocal512(object sender, RoutedEventArgs e) => await SaveToAppStorageAsync(512);

		private async void OnPickFileAndSave(object sender, RoutedEventArgs e) => await PickFileAndSaveAsync();

		private async Task SaveViaPickerAsync(int mb)
		{
			if (_busy)
			{
				Log("Busy - wait for the current run to finish.");
				return;
			}

			try
			{
				var picker = new FileSavePicker { SuggestedFileName = $"generated-{mb}mb" };
				picker.FileTypeChoices.Add("Binary", new List<string> { ".bin" });

				var file = await picker.PickSaveFileAsync();
				if (file is null)
				{
					Log("Picker cancelled.");
					return;
				}

				CachedFileManager.DeferUpdates(file);
				using (var outStream = await file.OpenStreamForWriteAsync())
				{
					await WriteGeneratedAsync(outStream, (long)mb * 1024 * 1024, $"picker {mb} MB");
				}
				await CachedFileManager.CompleteUpdatesAsync(file);
				Log($"Completed picker save of {mb} MB.");
			}
			catch (Exception ex)
			{
				Log($"FAILED: {ex.GetType().Name}: {ex.Message}");
			}
		}

		private async Task SaveToAppStorageAsync(int mb)
		{
			if (_busy)
			{
				Log("Busy - wait for the current run to finish.");
				return;
			}

			try
			{
				var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
					$"large-write-{Guid.NewGuid():N}.bin", CreationCollisionOption.ReplaceExisting);
				try
				{
					using (var outStream = await file.OpenStreamForWriteAsync())
					{
						await WriteGeneratedAsync(outStream, (long)mb * 1024 * 1024, $"app-storage {mb} MB");
					}
				}
				finally
				{
					try
					{
						await file.DeleteAsync();
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				Log($"FAILED: {ex.GetType().Name}: {ex.Message}");
			}
		}

		private async Task PickFileAndSaveAsync()
		{
			if (_busy)
			{
				Log("Busy - wait for the current run to finish.");
				return;
			}

			try
			{
				var open = new FileOpenPicker();
				open.FileTypeFilter.Add("*");
				var source = await open.PickSingleFileAsync();
				if (source is null)
				{
					Log("Open cancelled.");
					return;
				}

				var save = new FileSavePicker { SuggestedFileName = source.Name };
				var extension = Path.GetExtension(source.Name);
				save.FileTypeChoices.Add("File", new List<string> { string.IsNullOrEmpty(extension) ? ".bin" : extension });
				var destination = await save.PickSaveFileAsync();
				if (destination is null)
				{
					Log("Save cancelled.");
					return;
				}

				_busy = true;
				Log($"--- Copying '{source.Name}' via OpenStreamForWriteAsync ---");
				var startMem = GetMemoryUsageMb();
				var peakMem = startMem;

				using var input = await source.OpenStreamForReadAsync();
				var buffer = new byte[ChunkSize];
				long written = 0;
				long nextReport = 0;

				CachedFileManager.DeferUpdates(destination);
				using (var outStream = await destination.OpenStreamForWriteAsync())
				{
					int read;
					while ((read = await input.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
					{
						await outStream.WriteAsync(buffer.AsMemory(0, read));
						written += read;
						if (written >= nextReport)
						{
							peakMem = Math.Max(peakMem, GetMemoryUsageMb());
							MemText.Text = $"copied {written / (1024 * 1024)} MB   |   heap {peakMem} MB (start {startMem})";
							nextReport += 64L * 1024 * 1024;
							await Task.Yield();
						}
					}
					await outStream.FlushAsync();
				}
				await CachedFileManager.CompleteUpdatesAsync(destination);
				Log($"DONE: copied {written / (1024 * 1024)} MB. heap {startMem} -> {peakMem} MB.");
			}
			catch (Exception ex)
			{
				Log($"FAILED: {ex.GetType().Name}: {ex.Message}");
			}
			finally
			{
				_busy = false;
			}
		}

		private async Task WriteGeneratedAsync(Stream outStream, long total, string label)
		{
			_busy = true;
			try
			{
				Log($"--- {label}: writing via OpenStreamForWriteAsync ---");
				var startMem = GetMemoryUsageMb();
				var peakMem = startMem;

				// One reused chunk buffer - the app never holds more than 2 MB itself.
				var buffer = new byte[ChunkSize];
				long written = 0;
				long nextReport = 0;

				while (written < total)
				{
					var count = (int)Math.Min(ChunkSize, total - written);
					await outStream.WriteAsync(buffer.AsMemory(0, count));
					written += count;

					if (written >= nextReport)
					{
						peakMem = Math.Max(peakMem, GetMemoryUsageMb());
						Progress.Value = (double)written / total * 100;
						MemText.Text = $"written {written / (1024 * 1024)} MB   |   heap {peakMem} MB (start {startMem})";
						nextReport += 64L * 1024 * 1024;
						await Task.Yield();
					}
				}
				await outStream.FlushAsync();

				peakMem = Math.Max(peakMem, GetMemoryUsageMb());
				Log($"DONE: wrote {written / (1024 * 1024)} MB. heap {startMem} -> {peakMem} MB.");
			}
			finally
			{
				_busy = false;
			}
		}

		private static long GetMemoryUsageMb()
		{
			// Only a hint: on WebAssembly the payload is staged in JS-side buffers, which the
			// managed heap does not reflect. Measure real growth in the browser's dev tools.
			try
			{
				return GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
			}
			catch
			{
				return -1;
			}
		}

		private void Log(string message)
		{
			LogBox.Text = message + "\n" + LogBox.Text;
		}
	}
}
