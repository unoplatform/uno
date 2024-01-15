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
using Microsoft.UI.Xaml.Navigation;

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

		public ICommand ClearCommand => GetOrCreateCommand(Clear);

		public ICommand CopyCommand => GetOrCreateCommand(Copy);

		public ICommand PasteCommand => GetOrCreateCommand(Paste);

		public ICommand FlushCommand => GetOrCreateCommand(Flush);

		public ICommand ToggleContentChangedCommand => GetOrCreateCommand(ToggleContentChange);

		private void Clear() => Clipboard.Clear();

		private void Copy()
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(Text);
			Clipboard.SetContent(dataPackage);
		}

		private async void Paste()
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
	}
}
