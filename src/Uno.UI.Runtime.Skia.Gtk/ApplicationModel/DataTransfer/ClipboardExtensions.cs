#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Clipboard = Gtk.Clipboard;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.ApplicationModel.DataTransfer
{
	internal class ClipboardExtensions : IClipboardExtension
	{
		public event EventHandler<object> ContentChanged;

		static readonly Atom HtmlContent = Atom.Intern("text/html", false);
		static readonly Atom RtfContent = Atom.Intern("text/rtf", false);
		static readonly Atom GnomeCopiedFilesContent = Atom.Intern("x-special/gnome-copied-files", false);

		private readonly Clipboard _clipboard;

		public ClipboardExtensions(object owner)
		{
			_clipboard = Clipboard.GetDefault(GtkHost.Window.Display);
		}

		public void Clear() => _clipboard.Clear();
		public void Flush()
		{
			_clipboard.CanStore = null;
			_clipboard.Store();
		}

		public DataPackageView GetContent()
		{
			var dataPackage = new DataPackage();

			if (_clipboard.WaitIsImageAvailable())
			{
				dataPackage.SetDataProvider(StandardDataFormats.Bitmap, async ct =>
				{
					var image = _clipboard.WaitForImage();
					var data = image.SaveToBuffer("bmp");
					var stream = new MemoryStream(data);
					return RandomAccessStreamReference.CreateFromStream(stream.AsRandomAccessStream());
				});
			}
			if (_clipboard.WaitIsTargetAvailable(HtmlContent))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Html, async ct =>
				{
					var selectionData = _clipboard.WaitForContents(HtmlContent);
					return Encoding.UTF8.GetString(selectionData.Data);
				});
			}
			if (_clipboard.WaitIsTargetAvailable(RtfContent))
			{
				dataPackage.SetDataProvider(StandardDataFormats.Rtf, async ct =>
				{
					var selectionData = _clipboard.WaitForContents(RtfContent);
					return Encoding.UTF8.GetString(selectionData.Data);
				});
			}
			if (_clipboard.WaitIsTextAvailable())
			{
				dataPackage.SetDataProvider(StandardDataFormats.Text, async ct =>
				{
					return _clipboard.WaitForText();
				});
			}
			if (_clipboard.WaitIsUrisAvailable())
			{
				// We have to get the actual Uris before determining
				// the Uri types. Therefore, we will not use SetDataProvider here.

				// Gtk documentation https://developer.gnome.org/gtk3/stable/gtk3-Clipboards.html#gtk-clipboard-wait-for-uris
				// says that the function returns an **array** of strings,
				// while GTK# just exposes 1 string?
				var uris = _clipboard.WaitForUris();

				try
				{
					if (uris != null)
					{
						DataPackage.SeparateUri(
							uris,
							out string webLink,
							out string applicationLink);

						var clipWebLink = webLink != null ? new Uri(webLink) : null;
						var clipApplicationLink = applicationLink != null ? new Uri(applicationLink) : null;
						var clipUri = new Uri(uris);

						if (clipWebLink != null)
						{
							dataPackage.SetWebLink(clipWebLink);
						}
						if (clipApplicationLink != null)
						{
							dataPackage.SetApplicationLink(clipApplicationLink);
						}
						if (clipUri != null)
						{
							dataPackage.SetUri(clipUri);
						}
					}
				}
				catch (UriFormatException e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError($"Invalid URI on clipboard: {uris}", e);
					}
				}
			}
			if (_clipboard.WaitIsTargetAvailable(GnomeCopiedFilesContent))
			{
				dataPackage.SetDataProvider(StandardDataFormats.StorageItems, async ct =>
				{
					// Got some hint here: https://stackoverflow.com/a/7356732
					// This function does NOT work on distros using Nautilus (the popular Ubuntu), 
					// as the copied files are visible as **plain text** to all other
					// programs, and registers no special data type for files on clipboard.
					var data = _clipboard.WaitForContents(GnomeCopiedFilesContent);
					// The first element is the command (cut, copy,...)
					var dataList = Encoding.UTF8.GetString(data.Data).Split('\n').Skip(1);

					var storageItemList = new List<IStorageItem>();

					foreach (var fileUriString in dataList)
					{
						var path = Uri.UnescapeDataString(fileUriString.Substring("file://".Length));

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

			CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.High,
				() => SetContentAsync(content));
		}

		private async Task SetContentAsync(DataPackage content)
		{
			var data = content?.GetView();
			var targetList = new TargetList();
			var targetStrings = new List<string>();

			bool CheckFormat(string format, out uint id)
			{
				if (data?.Contains(format) ?? false)
				{
					id = (uint)targetStrings.Count;
					targetStrings.Add(format);
					return true;
				}
				id = 0;
				return false;
			}

			async void SetDataNative(Clipboard clipboard, SelectionData nativeData, uint info)
			{
				var format = targetStrings[(int)info];

				var uris = new List<string>();

				// Cannot use switch here, these strings are not constants!!!
				if (format == StandardDataFormats.Text)
				{
					nativeData.Text = await data.GetTextAsync();
				}
				else if (format == StandardDataFormats.Bitmap)
				{
					var streamRef = await data.GetBitmapAsync();
					var uwpStream = await streamRef.OpenReadAsync();
					var stream = uwpStream.AsStreamForRead();
					nativeData.SetPixbuf(new Pixbuf(stream));
				}
				else if (format == StandardDataFormats.Html)
				{
					var htmlString = await data.GetHtmlFormatAsync();
					nativeData.Set(HtmlContent, 8, Encoding.UTF8.GetBytes(htmlString));
				}
				else if (format == StandardDataFormats.Rtf)
				{
					var rtfString = await data.GetRtfAsync();
					nativeData.Set(RtfContent, 8, Encoding.UTF8.GetBytes(rtfString));
				}
				else if (format == StandardDataFormats.StorageItems)
				{
					var items = await data.GetStorageItemsAsync();
					var builder = new StringBuilder();

					builder.AppendLine("copy");
					foreach (var item in items)
					{
						var path = item.Path;
						builder.AppendLine(FileUriHelper.UrlEncode(path));
					}

					nativeData.Set(GnomeCopiedFilesContent, 8, Encoding.UTF8.GetBytes(builder.ToString().Trim()));
				}
				else if (format == StandardDataFormats.ApplicationLink)
				{
					uris.Add((await data.GetApplicationLinkAsync()).ToString());
				}
				else if (format == StandardDataFormats.WebLink)
				{
					uris.Add((await data.GetWebLinkAsync()).ToString());
				}
				else if (format == StandardDataFormats.Uri)
				{
					uris.Add((await data.GetUriAsync()).ToString());
				}

				if (uris.Count != 0)
				{
					nativeData.SetUris(uris.ToArray());
				}
			}

			uint id;
			if (CheckFormat(StandardDataFormats.Text, out id))
			{
				targetList.AddTextTargets(id);
			}
			if (CheckFormat(StandardDataFormats.Bitmap, out id))
			{
				targetList.AddImageTargets(id, true);
			}
			if (CheckFormat(StandardDataFormats.Html, out id))
			{
				targetList.Add(HtmlContent, 0, id);
			}
			if (CheckFormat(StandardDataFormats.Rtf, out id))
			{
				targetList.Add(RtfContent, 0, id);
			}
			if (CheckFormat(StandardDataFormats.StorageItems, out id))
			{
				targetList.Add(GnomeCopiedFilesContent, 0, id);
			}
			if (CheckFormat(StandardDataFormats.ApplicationLink, out id))
			{
				targetList.AddUriTargets(id);
			}
			if (CheckFormat(StandardDataFormats.WebLink, out id))
			{
				targetList.AddUriTargets(id);
			}
			if (CheckFormat(StandardDataFormats.Uri, out id))
			{
				targetList.AddUriTargets(id);
			}

			_clipboard.SetWithData((TargetEntry [])targetList, SetDataNative, (clipboard) => { });
		}

		public void StartContentChanged()
		{
			_clipboard.OwnerChange += Clipboard_OwnerChange;
		}

		public void StopContentChanged()
		{
			_clipboard.OwnerChange -= Clipboard_OwnerChange;
		}

		private void Clipboard_OwnerChange(object o, OwnerChangeArgs args)
		{
			ContentChanged?.Invoke(this, args);
		}
	}
}
