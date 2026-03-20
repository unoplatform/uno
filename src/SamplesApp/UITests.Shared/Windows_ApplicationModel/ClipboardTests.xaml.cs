using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Streams;

namespace UITests.Windows_ApplicationModel
{
	[Sample("Windows.ApplicationModel", ViewModelType = typeof(ClipboardTestsViewModel))]
	public sealed partial class ClipboardTests : Page
	{
		public ClipboardTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += ClipboardTests_DataContextChanged;
		}

		internal ClipboardTestsViewModel Model { get; private set; }

		private void ClipboardTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = (ClipboardTestsViewModel)args.NewValue;
		}
	}

	internal class ClipboardTestsViewModel : ViewModelBase
	{
		private bool _isObservingContentChanged = false;
		private string _lastContentChangedDate = "";
		private string _text = "";
		private string _statusText = "";
		private string _pastedContent = "";
		private BitmapSource _bmp;

		public ClipboardTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			Disposables.Add(Disposable.Create(() =>
			{
				if (_isObservingContentChanged)
				{
					Clipboard.ContentChanged -= Clipboard_ContentChanged;
				}
			}));
		}

		public bool IsObservingContentChanged
		{
			get => _isObservingContentChanged;
			set
			{
				_isObservingContentChanged = value;
				RaisePropertyChanged();
			}
		}

		public string LastContentChangedDate
		{
			get => _lastContentChangedDate;
			set
			{
				_lastContentChangedDate = value;
				RaisePropertyChanged();
			}
		}

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				RaisePropertyChanged();
			}
		}

		public string StatusText
		{
			get => _statusText;
			set
			{
				_statusText = value;
				RaisePropertyChanged();
			}
		}

		public string PastedContent
		{
			get => _pastedContent;
			set
			{
				_pastedContent = value;
				RaisePropertyChanged();
			}
		}

		public BitmapSource Bitmap
		{
			get => _bmp;
			private set
			{
				_bmp = value;
				RaisePropertyChanged();
			}
		}

		#region ICommands
		public ICommand CopyCommand => GetOrCreateCommand(Copy);

		public ICommand CopyHtmlCommand => GetOrCreateCommand(CopyHtml);

		public ICommand CopyImageCommand => GetOrCreateCommand(CopyImage);

		public ICommand PasteTextCommand => GetOrCreateCommand(PasteText);

		public ICommand PasteHtmlCommand => GetOrCreateCommand(PasteHtml);

		public ICommand PasteStorageItemsCommand => GetOrCreateCommand(PasteStorageItems);

		public ICommand PasteImageCommand => GetOrCreateCommand(PasteImage);

		public ICommand ListAvailableFormatsCommand => GetOrCreateCommand(ListAvailableFormats);

		public ICommand ClearCommand => GetOrCreateCommand(Clear);

		public ICommand FlushCommand => GetOrCreateCommand(Flush);

		public ICommand ToggleContentChangedCommand => GetOrCreateCommand(ToggleContentChange);
		#endregion

		private void Copy()
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(Text);
			Clipboard.SetContent(dataPackage);
		}

		private void CopyHtml()
		{
			DataPackage dataPackage = new DataPackage();
			// Create HTML from the text, making it bold as a demo
			// Use HTML encoding to prevent XSS
			var encodedText = WebUtility.HtmlEncode(Text);
			var html = $"<p><strong>{encodedText}</strong></p>";
			dataPackage.SetHtmlFormat(html);
			dataPackage.SetText(Text); // Also set plain text as fallback
			Clipboard.SetContent(dataPackage);
		}

		private async void CopyImage()
		{
			var dataPackage = new DataPackage();
			var imageUri = OperatingSystem.IsBrowser()
				// for browser, the primary supported format is image/png
				? new Uri("ms-appx:///Assets/Formats/uno-overalls.png")
				: new Uri("ms-appx:///Assets/Formats/uno-overalls.bmp");
			var imageFile = await StorageFile.GetFileFromApplicationUriAsync(imageUri);
			dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(imageFile));
			Clipboard.SetContent(dataPackage);
		}

		private async void PasteText()
		{
			var package = Clipboard.GetContent();
			UpdateStatusAndClearOldContents(package);

			PastedContent = await package.GetTextAsync();
		}

		private async void PasteHtml()
		{
			var package = Clipboard.GetContent();
			UpdateStatusAndClearOldContents(package);

			if (package.Contains(StandardDataFormats.Html))
			{
				var html = await package.GetHtmlFormatAsync();
				// Truncate long HTML for better display
				var displayHtml = html.Length > 200 ? html[..200] + "..." : html;

				PastedContent = $"HTML: {displayHtml}";
			}
			else
			{
				PastedContent = "No HTML content in clipboard";
			}
		}

		private async void PasteStorageItems()
		{
			var package = Clipboard.GetContent();
			UpdateStatusAndClearOldContents(package);

			if (package.Contains(StandardDataFormats.StorageItems))
			{
				PastedContent = string.Join("\n", (await package.GetStorageItemsAsync()).Select(si => si.Path));
			}
			else
			{
				PastedContent = "No StorageItems content in clipboard";
			}
		}

		private async void PasteImage()
		{
			var package = Clipboard.GetContent();
			UpdateStatusAndClearOldContents(package);

			var formats = new[]
			{
				"image/png",
				"image/jpeg"
			};

			foreach (var format in formats)
			{
				if (package.Contains(format))
				{
					if (await package.GetDataAsync(format) is byte[] bytes)
					{
						var fileName = Path.GetTempPath() + Guid.NewGuid() + "." + format.Split("/")[1];
						await File.WriteAllBytesAsync(fileName, bytes);
						var bitmapImage = new BitmapImage(new Uri(fileName));
						Bitmap = bitmapImage;

						return;
					}
				}
			}

			if (Bitmap is null && package.Contains(StandardDataFormats.Bitmap))
			{
				var bitmapReference = await package.GetBitmapAsync();
				var bitmapStream = await bitmapReference.OpenReadAsync();

				var bitmapImage = new BitmapImage();
				bitmapImage.SetSource(bitmapStream);

				Bitmap = bitmapImage;
			}
		}

		private void ListAvailableFormats()
		{
			var package = Clipboard.GetContent();
			UpdateStatusAndClearOldContents(package);
		}

		private void Clear() => Clipboard.Clear();

		private void Flush() => Clipboard.Flush();

		private void ToggleContentChange()
		{
			IsObservingContentChanged = !IsObservingContentChanged;
			if (IsObservingContentChanged)
			{
				Clipboard.ContentChanged += Clipboard_ContentChanged;
			}
			else
			{
				Clipboard.ContentChanged -= Clipboard_ContentChanged;
			}
		}

		private void Clipboard_ContentChanged(object sender, object e) => LastContentChangedDate = Timestamp;

		private void UpdateStatusAndClearOldContents(DataPackageView package)
		{
			StatusText = $"{Timestamp} available formats: {string.Join(", ", package.AvailableFormats)}";
			PastedContent = null;
			Bitmap = null;
		}

		private static string Timestamp => DateTime.Now.ToString("HH\\:mm\\:ss");
	}
}
