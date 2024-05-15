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
		private BitmapImage _bmp;

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

		public BitmapImage Bitmap
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

		public ICommand PasteTextCommand => GetOrCreateCommand(PasteText);

		public ICommand PasteImageCommand => GetOrCreateCommand(PasteImage);

		public ICommand FlushCommand => GetOrCreateCommand(Flush);

		public ICommand ToggleContentChangedCommand => GetOrCreateCommand(ToggleContentChange);

		private void Clear() => Clipboard.Clear();

		private void Copy()
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(Text);
			Clipboard.SetContent(dataPackage);
		}

		private async void PasteText()
		{
			var content = Clipboard.GetContent();
			Text = await content.GetTextAsync();
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
						var ims = new InMemoryRandomAccessStream();
						DataWriter dataWriter = new DataWriter(ims);
						dataWriter.WriteBytes(bytes);
						await dataWriter.StoreAsync();
						ims.Seek(0);

						var bitmapImage = new BitmapImage();
						bitmapImage.SetSource(ims);
						Bitmap = bitmapImage;
						return;
					}
				}
			}

			if (dataPackageView.Contains(StandardDataFormats.Bitmap))
			{
				var bitmapReference = await dataPackageView.GetBitmapAsync();
				var stream = await bitmapReference.OpenReadAsync();
				var bitmapImage = new BitmapImage();
				await bitmapImage.SetSourceAsync(stream);
				Bitmap = bitmapImage;
			}
		}
	}
}
