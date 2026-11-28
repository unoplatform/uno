using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognizer : IDisposable
{
	private ISpeechRecognizerExtension _extension;
	private bool _disposed;

	private void InitializeSpeechRecognizer()
	{
		if (_extension is not null)
		{
			_extension.HypothesisGenerated -= OnExtensionHypothesisGenerated;
			_extension.StateChanged -= OnExtensionStateChanged;
			_extension.Dispose();
			_extension = null;
		}

		if (ApiExtensibility.CreateInstance<ISpeechRecognizerExtension>(this, out var extension))
		{
			_extension = extension;
			_extension.HypothesisGenerated += OnExtensionHypothesisGenerated;
			_extension.StateChanged += OnExtensionStateChanged;
			_extension.Initialize(CurrentLanguage, Timeouts);
		}
	}

	public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(SpeechRecognizer), "The object has been closed.");
		}

		if (_extension is null)
		{
			throw new NotSupportedException("SpeechRecognizer is not available on this platform.");
		}

		return AsyncOperation.FromTask(async ct =>
		{
			var (text, alternates) = await _extension.RecognizeAsync();
			return new SpeechRecognitionResult()
			{
				Text = text ?? string.Empty,
				Alternates = alternates?.Select(a => new SpeechRecognitionResult() { Text = a }).ToList(),
			};
		});
	}

	public IAsyncAction StopRecognitionAsync()
	{
		if (_extension is null)
		{
			return Task.CompletedTask.AsAsyncAction();
		}

		return AsyncAction.FromTask(_ => _extension.StopAsync());
	}

	public void Dispose()
	{
		_disposed = true;
		if (_extension is not null)
		{
			_extension.HypothesisGenerated -= OnExtensionHypothesisGenerated;
			_extension.StateChanged -= OnExtensionStateChanged;
			_extension.Dispose();
			_extension = null;
		}
	}

	private void OnExtensionHypothesisGenerated(object sender, string text) => OnHypothesisGenerated(text);

	private void OnExtensionStateChanged(object sender, SpeechRecognizerState state) => OnStateChanged(state);
}
