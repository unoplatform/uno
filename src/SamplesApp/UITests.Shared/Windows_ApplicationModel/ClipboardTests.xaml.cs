using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	[SampleControlInfo("Windows.ApplicationModel", viewModelType: typeof(ClipboardTestsViewModel))]
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

		public BitmapSource Bitmap
		{
			get => _bmp;
			private set
			{
				_bmp = value;
				RaisePropertyChanged();
			}
		}

		public ICommand ClearCommand => GetOrCreateCommand(Clear);

		public ICommand CopyCommand => GetOrCreateCommand(Copy);

		public ICommand CopyHtmlCommand => GetOrCreateCommand(CopyHtml);

		public ICommand CopyImageCommand => GetOrCreateCommand(CopyImage);

		public ICommand PasteTextCommand => GetOrCreateCommand(PasteText);

		public ICommand PasteHtmlCommand => GetOrCreateCommand(PasteHtml);

		public ICommand PasteImageCommand => GetOrCreateCommand(PasteImage);

		public ICommand PasteStorageItemsCommand => GetOrCreateCommand(PasteStorageItems);

		public ICommand FlushCommand => GetOrCreateCommand(Flush);

		public ICommand ToggleContentChangedCommand => GetOrCreateCommand(ToggleContentChange);

		private void Clear() => Clipboard.Clear();

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
			var html = $"<p><strong>{Text}</strong></p>";
			dataPackage.SetHtmlFormat(html);
			dataPackage.SetText(Text); // Also set plain text as fallback
			Clipboard.SetContent(dataPackage);
		}

		private async void PasteText()
		{
			var content = Clipboard.GetContent();
			Text = await content.GetTextAsync();
		}

		private async void PasteHtml()
		{
			var content = Clipboard.GetContent();
			if (content.Contains(StandardDataFormats.Html))
			{
				var html = await content.GetHtmlFormatAsync();
				Text = $"HTML: {html}";
			}
			else
			{
				Text = "No HTML content in clipboard";
			}
		}

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

		private void Clipboard_ContentChanged(object sender, object e)
		{
			LastContentChangedDate = DateTime.UtcNow.ToLongTimeString();
		}

		private async void PasteImage()
		{
			var dataPackageView = Clipboard.GetContent();

			if (dataPackageView is null)
			{
				return;
			}

			var formats = new[]
			{
				"image/png",
				"image/jpeg"
			};

			foreach (var format in formats)
			{
				if (dataPackageView.Contains(format))
				{
					if (await dataPackageView.GetDataAsync("image/png") is byte[] bytes)
					{
						var fileName = Path.GetTempPath() + Guid.NewGuid() + "." + format.Split("/")[1];
						await File.WriteAllBytesAsync(fileName, bytes);
						var bitmapImage = new BitmapImage(new Uri(fileName));
						Bitmap = bitmapImage;
					}
				}
			}

			if (Bitmap is null && dataPackageView.Contains(StandardDataFormats.Bitmap))
			{
				var bitmapReference = await dataPackageView.GetBitmapAsync();
				var bitmapImage = new BitmapImage();
				bitmapImage.SetSource(await bitmapReference.OpenReadAsync());
				Bitmap = bitmapImage;
			}
		}

		private void CopyImage()
		{
			var dataPackage = new DataPackage();
			dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Formats/uno-overalls.bmp")));
			Clipboard.SetContent(dataPackage);
		}

		private async void PasteStorageItems()
		{
			var dataPackageView = Clipboard.GetContent();

			if (dataPackageView is null)
			{
				return;
			}

			if (dataPackageView.Contains(StandardDataFormats.StorageItems))
			{
				Text = string.Join("\n", (await dataPackageView.GetStorageItemsAsync()).Select(si => si.Path));
			}
		}
	}
}
