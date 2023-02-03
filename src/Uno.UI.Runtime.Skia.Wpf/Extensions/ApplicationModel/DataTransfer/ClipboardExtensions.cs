using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Uno.ApplicationModel.DataTransfer;
using Uno.UI.Skia.Platform;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Clipboard = System.Windows.Clipboard;

namespace Uno.Extensions.ApplicationModel.DataTransfer
{
	internal class ClipboardExtensions : IClipboardExtension
	{
		const int WM_CLIPBOARDUPDATE = 0x031D;

		private readonly WpfHost _host;
		private HwndSource _hwndSource;
		private bool _pendingStartContentChanged;

		public event EventHandler<object> ContentChanged;

		public ClipboardExtensions(object owner)
		{
			_host = WpfHost.Current;

			// This class may be accessed before the Window is loaded
			// if the Clipboard is somehow accessed really early.
			if (_host.IsLoaded)
			{
				HostLoaded(null, null);
			}
			else
			{
				// Hook for native events
				_host.Loaded += HostLoaded;
			}

			void HostLoaded(object sender, RoutedEventArgs e)
			{
				_host.Loaded -= HostLoaded;

				var win = Window.GetWindow(_host);

				var fromDependencyObject = PresentationSource.FromDependencyObject(win);
				_hwndSource = fromDependencyObject as HwndSource;

				if (_pendingStartContentChanged)
				{
					StartContentChanged();
				}
			}
		}

		public void StartContentChanged()
		{
			if (_hwndSource != null)
			{
				_hwndSource.AddHook(OnWmMessage);
				ClipboardNativeFunctions.AddClipboardFormatListener(_hwndSource.Handle);
			}
			else
			{
				// Signals the app to hook when it's ready
				_pendingStartContentChanged = true;
			}
		}

		public void StopContentChanged()
		{
			if (_hwndSource != null)
			{
				ClipboardNativeFunctions.RemoveClipboardFormatListener(_hwndSource.Handle);
				_hwndSource.RemoveHook(OnWmMessage);
			}
			else
			{
				_pendingStartContentChanged = false;
			}
		}

		public void Flush() => Clipboard.Flush();

		public void Clear() => Clipboard.Clear();

		public DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			if (Clipboard.ContainsImage())
			{
				dataPackage.SetDataProvider(StandardDataFormats.Bitmap, ct =>
				{
					var bitmap = Clipboard.GetImage();
					var bitmapStream = new MemoryStream();

					var bitmapEncoder = new BmpBitmapEncoder();
					bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));
					bitmapEncoder.Save(bitmapStream);

					// Letting a MemoryStream run around does not cause problems.
					// The GC will take care of it, just like a byte[].
					return Task.FromResult<object>(RandomAccessStreamReference.CreateFromStream(bitmapStream.AsRandomAccessStream()));
				});
			}
			if (Clipboard.ContainsText())
			{
				// Copying significant amounts of text still makes Clipboard.GetText() slow, so
				// we'll still use the SetDataProvider
				dataPackage.SetDataProvider(StandardDataFormats.Text, ct =>
				{
					return Task.FromResult<object>(Clipboard.GetText());
				});
			}
			if (Clipboard.ContainsData(DataFormats.Html))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Html, ct =>
				{
					return Task.FromResult<object>(Clipboard.GetData(DataFormats.Html));
				});
			}
			if (Clipboard.ContainsData(DataFormats.Rtf))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Rtf, ct =>
				{
					return Task.FromResult<object>(Clipboard.GetData(DataFormats.Rtf));
				});
			}
			if (Clipboard.ContainsFileDropList())
			{
				dataPackage.SetDataProvider(StandardDataFormats.StorageItems, async ct =>
				{
					var list = Clipboard.GetFileDropList();
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

			Clipboard.SetDataObject(wpfData);
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
