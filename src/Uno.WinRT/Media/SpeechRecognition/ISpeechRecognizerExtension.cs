using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Globalization;

namespace Windows.Media.SpeechRecognition;

internal interface ISpeechRecognizerExtension : IDisposable
{
	event EventHandler<string> HypothesisGenerated;

	event EventHandler<SpeechRecognizerState> StateChanged;

	void Initialize(Language language, SpeechRecognizerTimeouts timeouts);

	Task<(string text, IReadOnlyList<string> alternates)> RecognizeAsync();

	Task StopAsync();
}
