using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using Microsoft.Extensions.Logging;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Clipboard = Gtk.Clipboard;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.ApplicationModel.DataTransfer
{
	internal class ClipboardExtensions : IClipboardExtension
	{
		public event EventHandler<object> ContentChanged;

		static readonly Atom HtmlContent = Atom.Intern("text/html", false);
		static readonly Atom RtfContent = Atom.Intern("text/rtf", false);
		static readonly Atom GnomeCopiedFilesContent = Atom.Intern("x-special/gnome-copied-files", false);

		private Clipboard _clipboard;

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

			if (data?.Contains(StandardDataFormats.Text) ?? false)
			{
				_clipboard.Text = await data.GetTextAsync();
			}
			if (data?.Contains(StandardDataFormats.Bitmap) ?? false)
			{
				var streamRef = await data.GetBitmapAsync();
				var runtimeStream = await streamRef.OpenReadAsync();
				var stream = runtimeStream.AsStreamForRead();
				_clipboard.Image = new Pixbuf(stream);
			}
			if (data?.Contains(StandardDataFormats.Html) ?? false)
			{
				var htmlString = await data.GetHtmlFormatAsync();
				_clipboard.SetWithData(new[] { new TargetEntry("text/html", 0, 0) },
					(clipboard, selection, info) =>
					{
						selection.Set(HtmlContent, 8, Encoding.UTF8.GetBytes(htmlString));
					},
					(clipboard) => { });
			}
			if (data?.Contains(StandardDataFormats.Rtf) ?? false)
			{
				var rtfString = await data.GetRtfAsync();
				_clipboard.SetWithData(new[] { new TargetEntry("text/rtf", 0, 0) },
					(clipboard, selection, info) =>
					{
						selection.Set(RtfContent, 8, Encoding.UTF8.GetBytes(rtfString));
					},
					(clipboard) => { });
			}
			if (data?.Contains(StandardDataFormats.StorageItems) ?? false)
			{
				var items = await data?.GetStorageItemsAsync();
				var builder = new StringBuilder();

				builder.AppendLine("copy");
				foreach (var item in items)
				{
					var path = item.Path;
					builder.AppendLine(UrlEncode(path));
				}

				var target0 = new TargetEntry("x-special/gnome-copied-files", 0, 0);
				var target1 = new TargetEntry("text/uri-list", 0, 0);

				_clipboard.SetWithData(new[] { target0, target1 },
				(clipboard, selection, info) =>
				{
					selection.Set(selection.Target, 8, Encoding.UTF8.GetBytes(builder.ToString().Trim()));
				},
				(clipboard) => { });
			}
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

		/// <summary>
		/// Encodes a file path to a file:// Url.
		/// While the built-in Uri class can handle this, it does not process files
		/// such as '/home/user/%51.txt' correctly.
		/// </summary>
		/// <param name="path">Path to the file</param>
		/// <returns>file:// url to the file</returns>
		private string UrlEncode(string path)
		{
			var uri = new StringBuilder();
			foreach (var ch in path)
			{
				if (('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ('0' <= ch && ch <= '9') ||
					"-._~".Contains(ch))
				{
					uri.Append(ch);
				}
				else if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
				{
					uri.Append('/');
				}
				else
				{
					var bytes = Encoding.UTF8.GetBytes(new[] { ch });
					foreach (var b in bytes)
					{
						uri.Append($"%{b:X2}");
					}
				}
			}
			return "file://" + uri.ToString();
		}
	}
}
