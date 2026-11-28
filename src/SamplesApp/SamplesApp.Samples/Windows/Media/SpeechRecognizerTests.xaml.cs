using System;
using System.Linq;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Media.SpeechRecognition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Windows_Media
{
	[Sample("Windows.Media", Name = "SpeechRecognizer", ViewModelType = typeof(SpeechRecognizerTestsViewModel), IsManualTest = true,
		Description = "Speak after pressing Recognize. On macOS, run the SamplesApp.app bundle so the permission prompts can be shown.")]
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

		private string _state = "";
		private string _hypothesis = "";
		private string _lastResult = "";
		private string _lastResultDetails = "";
		private string _log = "";

		public SpeechRecognizerTestsViewModel(UnitTestDispatcherCompat dispatcher)
			: base(dispatcher)
		{
			_speechRecognizer.HypothesisGenerated += OnHypothesisGenerated;
			_speechRecognizer.StateChanged += OnStateChanged;
			Disposables.Add(() =>
			{
				_speechRecognizer.HypothesisGenerated -= OnHypothesisGenerated;
				_speechRecognizer.StateChanged -= OnStateChanged;
				_speechRecognizer.Dispose();
			});
		}

		public ICommand RecognizeCommand => GetOrCreateCommand(Recognize);

		public ICommand StopCommand => GetOrCreateCommand(() => Run(async () => await _speechRecognizer.StopRecognitionAsync()));

		public string State
		{
			get => _state;
			set
			{
				_state = value;
				RaisePropertyChanged();
			}
		}

		public string Hypothesis
		{
			get => _hypothesis;
			set
			{
				_hypothesis = value;
				RaisePropertyChanged();
			}
		}

		public string LastResult
		{
			get => _lastResult;
			set
			{
				_lastResult = value;
				RaisePropertyChanged();
			}
		}

		public string LastResultDetails
		{
			get => _lastResultDetails;
			set
			{
				_lastResultDetails = value;
				RaisePropertyChanged();
			}
		}

		public string Log
		{
			get => _log;
			set
			{
				_log = value;
				RaisePropertyChanged();
			}
		}

		private void Recognize() => Run(async () =>
		{
			var compilation = await _speechRecognizer.CompileConstraintsAsync();
			AppendLog($"Constraints compiled: {compilation.Status}");
			ShowResult(await _speechRecognizer.RecognizeAsync());
		});

		private void ShowResult(SpeechRecognitionResult result)
		{
			LastResult = result.Text;
			var alternates = string.Join(" | ", (result.GetAlternates(5) ?? Array.Empty<SpeechRecognitionResult>()).Skip(1).Select(alternate => alternate.Text));
			LastResultDetails = $"Status: {result.Status}, Confidence: {result.Confidence} ({result.RawConfidence:0.00}), " +
				$"Phrase: {result.PhraseStartTime:T} for {result.PhraseDuration.TotalSeconds:0.0}s" +
				(alternates.Length > 0 ? $", Alternates: {alternates}" : "");
		}

		private void OnHypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
			=> OnUIThread(() => Hypothesis = args.Hypothesis.Text);

		private void OnStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
			=> OnUIThread(() =>
			{
				State = $"State: {args.State}";
				AppendLog(args.State.ToString());
			});

		private void AppendLog(string message) => Log = message + Environment.NewLine + Log;

		private async void Run(Func<System.Threading.Tasks.Task> action)
		{
			try
			{
				await action();
			}
			catch (Exception ex)
			{
				AppendLog($"{ex.GetType().Name}: {ex.Message}");
			}
		}

		private async void OnUIThread(Action action)
			=> await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => action());
	}
}
