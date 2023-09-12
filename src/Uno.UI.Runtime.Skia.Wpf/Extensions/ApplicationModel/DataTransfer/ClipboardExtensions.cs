using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Uno.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.Wpf;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using WpfApplication = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;
using WpfFrameworkElement = System.Windows.FrameworkElement;

namespace Uno.Extensions.ApplicationModel.DataTransfer
{
	internal class ClipboardExtensions : IClipboardExtension
	{
		private const int WM_CLIPBOARDUPDATE = 0x031D;

		public ClipboardExtensions(object owner)
		{
		}

		public event EventHandler<object> ContentChanged;

		private HwndSource GetHwnd(WpfFrameworkElement host)
		{
			var fromDependencyObject = PresentationSource.FromDependencyObject(host as DependencyObject);
			return fromDependencyObject as HwndSource;
		}

		public void StartContentChanged()
		{
			if (WpfApplication.Current.MainWindow is not null)
			{
				RegisterClipboardListener();
			}
			else if (WpfHost.Current is not null)
			{
				// Signals the app to hook when it's ready
				//TODO:MZ: Replace somehow
				//WpfHost.Current.MainWindowShown += OnMainWindowShown;
			}
		}

		private void RegisterClipboardListener()
		{
			var hwndSource = GetHwnd(WpfApplication.Current.MainWindow);
			hwndSource.AddHook(OnWmMessage);
			ClipboardNativeFunctions.AddClipboardFormatListener(hwndSource.Handle);
		}

		private void OnMainWindowShown(object sender, EventArgs e)
		{
			RegisterClipboardListener();
			//TODO:MZ: Replace somehow
			//WpfHost.Current.MainWindowShown -= OnMainWindowShown;
		}

		public void StopContentChanged()
		{
			if (WpfApplication.Current.MainWindow is not null)
			{
				var hwndSource = GetHwnd(WpfApplication.Current.MainWindow);
				hwndSource.RemoveHook(OnWmMessage);
				ClipboardNativeFunctions.RemoveClipboardFormatListener(hwndSource.Handle);
			}
			else
			{
				//TODO:MZ: Replace somehow
				//WpfHost.Current.MainWindowShown -= OnMainWindowShown;
			}
		}

		public void Flush() => WpfClipboard.Flush();

		public void Clear() => WpfClipboard.Clear();

		public DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			if (WpfClipboard.ContainsImage())
			{
				dataPackage.SetDataProvider(StandardDataFormats.Bitmap, ct =>
				{
					var bitmap = WpfClipboard.GetImage();
					var bitmapStream = new MemoryStream();

					var bitmapEncoder = new BmpBitmapEncoder();
					bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));
					bitmapEncoder.Save(bitmapStream);

					// Letting a MemoryStream run around does not cause problems.
					// The GC will take care of it, just like a byte[].
					return Task.FromResult<object>(RandomAccessStreamReference.CreateFromStream(bitmapStream.AsRandomAccessStream()));
				});
			}
			if (WpfClipboard.ContainsText())
			{
				// Copying significant amounts of text still makes Clipboard.GetText() slow, so
				// we'll still use the SetDataProvider
				dataPackage.SetDataProvider(StandardDataFormats.Text, ct =>
				{
					return Task.FromResult<object>(WpfClipboard.GetText());
				});
			}
			if (WpfClipboard.ContainsData(DataFormats.Html))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Html, ct =>
				{
					return Task.FromResult<object>(WpfClipboard.GetData(DataFormats.Html));
				});
			}
			if (WpfClipboard.ContainsData(DataFormats.Rtf))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Rtf, ct =>
				{
					return Task.FromResult<object>(WpfClipboard.GetData(DataFormats.Rtf));
				});
			}
			if (WpfClipboard.ContainsFileDropList())
			{
				dataPackage.SetDataProvider(StandardDataFormats.StorageItems, async ct =>
				{
					var list = WpfClipboard.GetFileDropList();
					var storageItemList = new List<IStorageItem>(list.Count);
					foreach (var path in list)
					{
						var attr = File.GetAttributes(path);
						if (attr.HasFlag(global::System.IO.FileAttributes.Directory))
						{
							storageItemList.Add(await StorageFolder.GetFolderFromPathAsync(path));
						}
						else
						{
							storageItemList.Add(await StorageFile.GetFileFromPathAsync(path));
						}
					}
					return storageItemList;
				});
			}

			return dataPackage.GetView();
		}

		public void SetContent(DataPackage content)
		{
			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				() => _ = SetContentAsync(content));
		}

		private async Task SetContentAsync(DataPackage content)
		{
			var data = content?.GetView();
			var wpfData = new DataObject();

			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				wpfData.SetText(await data.GetTextAsync());
			}
			if (data?.Contains(StandardDataFormats.Bitmap) ?? false)
			{
				var streamRef = await data.GetBitmapAsync();
				var runtimeStream = await streamRef.OpenReadAsync();
				var stream = runtimeStream.AsStreamForRead();
				var image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = stream;
				image.EndInit();
				wpfData.SetImage(image);
			}
			if (data?.Contains(StandardDataFormats.Html) ?? false)
			{
				wpfData.SetData(DataFormats.Html, await data.GetHtmlFormatAsync());
			}
			if (data?.Contains(StandardDataFormats.Rtf) ?? false)
			{
				wpfData.SetData(DataFormats.Rtf, await data.GetRtfAsync());
			}
			if (data?.Contains(StandardDataFormats.StorageItems) ?? false)
			{
				var items = await data?.GetStorageItemsAsync();
				var list = new StringCollection();

				foreach (var item in items)
				{
					list.Add(item.Path);
				}

				wpfData.SetFileDropList(list);
			}

			WpfClipboard.SetDataObject(wpfData);
		}

		private IntPtr OnWmMessage(IntPtr hwnd, int msg, IntPtr wparamOriginal, IntPtr lparamOriginal, ref bool handled)
		{
			switch (msg)
			{
				case WM_CLIPBOARDUPDATE:
					{
						ContentChanged?.Invoke(this, EventArgs.Empty);
						handled = true;
						break;
					}
			}

			return IntPtr.Zero;
		}
	}
}
