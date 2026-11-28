#nullable enable

using System;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechContinuousRecognitionSession
{
	private readonly SpeechRecognizer _owner;

	internal SpeechContinuousRecognitionSession(SpeechRecognizer owner) => _owner = owner;

	// WinRT doesn't document its default; this is the documented default of the Unity wrapper around the same engine.
	public TimeSpan AutoStopSilenceTimeout { get; set; } = TimeSpan.FromSeconds(20);

	public event TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionCompletedEventArgs>? Completed;

	public event TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>? ResultGenerated;

	public IAsyncAction StartAsync() => StartAsync(SpeechContinuousRecognitionMode.Default);

	public IAsyncAction StartAsync(SpeechContinuousRecognitionMode mode)
		=> AsyncAction.FromTask(_ => _owner.StartContinuousRecognitionAsync(mode));

	public IAsyncAction StopAsync()
		=> AsyncAction.FromTask(_ => _owner.StopContinuousRecognitionAsync(discardPendingResults: false));

	public IAsyncAction CancelAsync()
		=> AsyncAction.FromTask(_ => _owner.StopContinuousRecognitionAsync(discardPendingResults: true));

	public IAsyncAction PauseAsync()
		=> AsyncAction.FromTask(_ => _owner.PauseContinuousRecognitionAsync());

	public void Resume() => _owner.ResumeContinuousRecognition();

	internal void RaiseResultGenerated(SpeechRecognitionResult result)
		=> ResultGenerated?.Invoke(this, new SpeechContinuousRecognitionResultGeneratedEventArgs(result));

	internal void RaiseCompleted(SpeechRecognitionResultStatus status)
		=> Completed?.Invoke(this, new SpeechContinuousRecognitionCompletedEventArgs(status));
}
