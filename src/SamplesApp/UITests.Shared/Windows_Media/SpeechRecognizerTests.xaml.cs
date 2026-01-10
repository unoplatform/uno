using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Windows_Media
{
	[Sample("Windows.Media", Name = "SpeechRecognizer", ViewModelType = typeof(SpeechRecognizerTestsViewModel))]
	public sealed partial class SpeechRecognizerTests : Page
	{
		public SpeechRecognizerTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += SpeechRecognizerTests_DataContextChanged;
		}

		internal SpeechRecognizerTestsViewModel Model { get; private set; }

		private void SpeechRecognizerTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as SpeechRecognizerTestsViewModel;
		}
	}

	internal class SpeechRecognizerTestsViewModel : ViewModelBase
	{
		private readonly SpeechRecognizer _speechRecognizer = new SpeechRecognizer(new Windows.Globalization.Language("en-US"));

		private string _lastResult = "";

		public SpeechRecognizerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher)
			: base(dispatcher)
		{
			_speechRecognizer.HypothesisGenerated += HypothesisGenerated;
			Disposables.Add(() =>
			{
				_speechRecognizer.HypothesisGenerated -= HypothesisGenerated;
				_speechRecognizer.Dispose();
			});
		}

		public ICommand RecognizeCommand => GetOrCreateCommand(Recognize);

		public string LastResult
		{
			get => _lastResult;
			set
			{
				_lastResult = value;
				RaisePropertyChanged();
			}
		}

		private async void Recognize()
		{
			await _speechRecognizer.CompileConstraintsAsync();
			var result = await _speechRecognizer.RecognizeAsync();
			LastResult = result.Text;
		}

		private async void HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				LastResult = args.Hypothesis.Text;
			});
		}
	}
}
