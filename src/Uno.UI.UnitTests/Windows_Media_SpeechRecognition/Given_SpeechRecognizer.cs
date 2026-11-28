#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace Uno.UI.Tests.Windows_Media_SpeechRecognition;

[UITestClass]
public class Given_SpeechRecognizer
{
	private FakeSpeechRecognizerExtension _engine = null!;
	private ManualTimeProvider _time = null!;
	private SpeechRecognizer _sut = null!;
	private List<SpeechRecognizerState> _states = null!;
	private List<string> _hypotheses = null!;

	[TestInitialize]
	public void Setup() => CreateRecognizer();

	[TestCleanup]
	public void Cleanup() => _sut.Dispose();

	[TestMethod]
	public void When_Created_Then_Timeouts_Match_WinUI_Defaults()
	{
		Assert.AreEqual(TimeSpan.FromSeconds(5), _sut.Timeouts.InitialSilenceTimeout);
		Assert.AreEqual(TimeSpan.FromMilliseconds(150), _sut.Timeouts.EndSilenceTimeout);
		Assert.AreEqual(TimeSpan.Zero, _sut.Timeouts.BabbleTimeout);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Phrase_Spoken_Then_States_Follow_WinUI_Sequence()
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		SpeakAndPause("hello world");
		Assert.AreEqual(SpeechRecognizerState.Processing, _sut.State);
		CollectionAssert.AreEqual(new[] { "Begin", "End" }, _engine.Calls);

		_engine.Complete("Hello world");
		var result = await recognition;

		Assert.AreEqual(SpeechRecognitionResultStatus.Success, result.Status);
		Assert.AreEqual("Hello world", result.Text);
		CollectionAssert.AreEqual(new[] { "hello world" }, _hypotheses);
		CollectionAssert.AreEqual(
			new[]
			{
				SpeechRecognizerState.Capturing,
				SpeechRecognizerState.SoundStarted,
				SpeechRecognizerState.SpeechDetected,
				SpeechRecognizerState.SoundEnded,
				SpeechRecognizerState.Processing,
				SpeechRecognizerState.Idle,
			},
			_states);
		Assert.AreEqual("StopAudio", _engine.Calls.Last());
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Speech_Continues_Then_End_Silence_Timeout_Is_Postponed()
	{
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseAudioLevel(FakeSpeechRecognizerExtension.LoudDecibels);
		_engine.RaiseSpeechDetected();

		// Speech alternates between syllables and short dips; the dips set the noise floor.
		for (var i = 1; i <= 40; i++)
		{
			_time.Advance(TimeSpan.FromMilliseconds(50));
			_engine.RaiseAudioLevel(i % 2 == 0 ? FakeSpeechRecognizerExtension.LoudDecibels : -45f);
		}

		Assert.IsFalse(_engine.Calls.Contains("End"));

		_time.Advance(TimeSpan.FromMilliseconds(150));
		Assert.IsTrue(_engine.Calls.Contains("End"));

		_engine.Complete("Still talking");
		Assert.AreEqual("Still talking", (await recognition).Text);
	}

	[TestMethod]
	public async Task When_No_Speech_Before_Initial_Silence_Timeout_Then_TimeoutExceeded()
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		_time.Advance(TimeSpan.FromSeconds(4.9));
		Assert.IsFalse(_engine.Calls.Contains("Cancel"));

		_time.Advance(TimeSpan.FromMilliseconds(100));
		Assert.IsTrue(_engine.Calls.Contains("Cancel"));

		_engine.CompleteCanceled();
		var result = await recognition;

		Assert.AreEqual(SpeechRecognitionResultStatus.TimeoutExceeded, result.Status);
		Assert.AreEqual(string.Empty, result.Text);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Only_Noise_Past_Babble_Timeout_Then_TimeoutExceeded()
	{
		_sut.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(1);
		var recognition = _sut.RecognizeAsync().AsTask();

		_engine.RaiseAudioLevel(FakeSpeechRecognizerExtension.LoudDecibels);
		_time.Advance(TimeSpan.FromSeconds(1));
		Assert.IsTrue(_engine.Calls.Contains("Cancel"));

		_engine.CompleteCanceled();
		Assert.AreEqual(SpeechRecognitionResultStatus.TimeoutExceeded, (await recognition).Status);
	}

	[TestMethod]
	public async Task When_Recognizing_Then_Second_Recognition_Throws()
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _sut.RecognizeAsync().AsTask());

		_engine.CompleteCanceled();
		await recognition;
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Permission_Not_Granted_Then_Privacy_Exception(bool isUsageDescriptionMissing)
	{
		_engine.Authorization = isUsageDescriptionMissing
			? SpeechRecognizerAuthorization.MissingUsageDescription
			: SpeechRecognizerAuthorization.Denied;

		var exception = await Assert.ThrowsExactlyAsync<COMException>(() => _sut.RecognizeAsync().AsTask());

		Assert.AreEqual(SpeechRecognizer.SpeechPrivacyPolicyNotAcceptedHResult, exception.HResult);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
		Assert.IsFalse(_engine.Calls.Contains("Begin"));
	}

	[TestMethod]
	[DataRow(SpeechRecognitionResultStatus.MicrophoneUnavailable)]
	[DataRow(SpeechRecognitionResultStatus.NetworkFailure)]
	public async Task When_Engine_Fails_Then_Status_Is_Reported(SpeechRecognitionResultStatus status)
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		_engine.CompleteError(status);
		var result = await recognition;

		Assert.AreEqual(status, result.Status);
		Assert.AreEqual(string.Empty, result.Text);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Language_Not_Supported_Then_TopicLanguageNotSupported()
	{
		_sut.Dispose();
		CreateRecognizer(languageSupported: false);

		Assert.AreEqual(SpeechRecognitionResultStatus.TopicLanguageNotSupported, (await _sut.CompileConstraintsAsync().AsTask()).Status);
		Assert.AreEqual(SpeechRecognitionResultStatus.TopicLanguageNotSupported, (await _sut.RecognizeAsync().AsTask()).Status);
		Assert.AreEqual(0, _engine.Calls.Count);
	}

	[TestMethod]
	public async Task When_Stopped_Before_Speech_Then_UserCanceled()
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		var stop = _sut.StopRecognitionAsync().AsTask();
		Assert.IsTrue(_engine.Calls.Contains("Cancel"));
		Assert.IsFalse(stop.IsCompleted);

		_engine.CompleteCanceled();
		await stop;

		Assert.AreEqual(SpeechRecognitionResultStatus.UserCanceled, (await recognition).Status);
	}

	[TestMethod]
	public async Task When_Stopped_During_Speech_Then_Final_Result_Is_Returned()
	{
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.RaiseHypothesis("partial");

		var stop = _sut.StopRecognitionAsync().AsTask();
		Assert.IsTrue(_engine.Calls.Contains("End"));
		Assert.AreEqual(SpeechRecognizerState.Processing, _sut.State);

		_engine.Complete("Partial phrase");
		await stop;

		var result = await recognition;
		Assert.AreEqual(SpeechRecognitionResultStatus.Success, result.Status);
		Assert.AreEqual("Partial phrase", result.Text);
	}

	[TestMethod]
	public async Task When_Operation_Canceled_Then_Recognition_Is_Canceled()
	{
		var operation = _sut.RecognizeAsync();

		operation.Cancel();
		Assert.IsTrue(_engine.Calls.Contains("Cancel"));
		_engine.CompleteCanceled();

		await Assert.ThrowsAsync<OperationCanceledException>(() => operation.AsTask());
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Alternates_Then_First_Alternate_Is_The_Result()
	{
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.Complete("Recognize speech", "Wreck a nice beach");

		var result = await recognition;
		var alternates = result.GetAlternates(5);

		Assert.AreEqual(2, alternates.Count);
		Assert.AreSame(result, alternates[0]);
		Assert.AreEqual("Wreck a nice beach", alternates[1].Text);
	}

	[TestMethod]
	public async Task When_Disposed_During_Recognition_Then_Recognition_Is_Canceled()
	{
		var recognition = _sut.RecognizeAsync().AsTask();

		_sut.Dispose();

		await Assert.ThrowsAsync<OperationCanceledException>(() => recognition);
		Assert.IsTrue(_engine.IsDisposed);
	}

	[TestMethod]
	public async Task When_Recognized_Then_Confidence_And_Phrase_Timing_Are_Reported()
	{
		var recognition = _sut.RecognizeAsync().AsTask();
		var audioStartTime = _time.GetUtcNow().AddMilliseconds(-200);
		_engine.RaiseSpeechDetected();
		_engine.Complete(
			new[]
			{
				new SpeechTranscription("Turn on the lights", 0.92, TimeSpan.FromSeconds(0.4), TimeSpan.FromSeconds(1.3)),
				new SpeechTranscription("Turn on the light", 0.55, TimeSpan.FromSeconds(0.4), TimeSpan.FromSeconds(1.2)),
			},
			audioStartTime);

		var result = await recognition;

		Assert.AreEqual(SpeechRecognitionConfidence.High, result.Confidence);
		Assert.AreEqual(0.92, result.RawConfidence);
		Assert.AreEqual(audioStartTime + TimeSpan.FromSeconds(0.4), result.PhraseStartTime);
		Assert.AreEqual(TimeSpan.FromSeconds(1.3), result.PhraseDuration);
		Assert.AreEqual(SpeechRecognitionConfidence.Medium, result.GetAlternates(2)[1].Confidence);
	}

	[TestMethod]
	[DataRow(0.95, SpeechRecognitionConfidence.High)]
	[DataRow(0.8, SpeechRecognitionConfidence.High)]
	[DataRow(0.79, SpeechRecognitionConfidence.Medium)]
	[DataRow(0.5, SpeechRecognitionConfidence.Medium)]
	[DataRow(0.2, SpeechRecognitionConfidence.Low)]
	[DataRow(0.0, SpeechRecognitionConfidence.Low)]
	public void When_Raw_Confidence_Then_Confidence_Level(double rawConfidence, SpeechRecognitionConfidence expected)
		=> Assert.AreEqual(expected, SpeechRecognitionResult.GetConfidence(rawConfidence));

	[TestMethod]
	public async Task When_Nothing_Recognized_Then_Confidence_Is_Rejected()
	{
		var start = _time.GetUtcNow();
		var recognition = _sut.RecognizeAsync().AsTask();
		_time.Advance(TimeSpan.FromSeconds(5));
		_engine.CompleteCanceled();

		var result = await recognition;

		Assert.AreEqual(SpeechRecognitionConfidence.Rejected, result.Confidence);
		Assert.AreEqual(start, result.PhraseStartTime);
		Assert.AreEqual(TimeSpan.Zero, result.PhraseDuration);
	}

	[TestMethod]
	public async Task When_Canceled_During_Permission_Prompt_Then_Recognition_Does_Not_Start()
	{
		var authorization = new TaskCompletionSource<SpeechRecognizerAuthorization>();
		_engine.PendingAuthorization = authorization;
		var operation = _sut.RecognizeAsync();

		operation.Cancel();
		authorization.SetResult(SpeechRecognizerAuthorization.Authorized);

		await Assert.ThrowsAsync<OperationCanceledException>(() => operation.AsTask());
		Assert.IsFalse(_engine.Calls.Contains("Begin"));
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Stopped_During_Permission_Prompt_Then_UserCanceled()
	{
		var authorization = new TaskCompletionSource<SpeechRecognizerAuthorization>();
		_engine.PendingAuthorization = authorization;
		var recognition = _sut.RecognizeAsync().AsTask();

		await _sut.StopRecognitionAsync().AsTask();
		authorization.SetResult(SpeechRecognizerAuthorization.Authorized);

		Assert.AreEqual(SpeechRecognitionResultStatus.UserCanceled, (await recognition).Status);
		Assert.IsFalse(_engine.Calls.Contains("Begin"));
	}

	[TestMethod]
	public async Task When_List_Constraint_Then_Matching_Command_Is_Returned()
	{
		var answers = new SpeechRecognitionListConstraint(new[] { "yes", "no", "maybe later" }, "answers");
		_sut.Constraints.Add(answers);

		Assert.AreEqual(SpeechRecognitionResultStatus.Success, (await _sut.CompileConstraintsAsync().AsTask()).Status);
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.Complete("Mabel later", "Maybe later.");

		var result = await recognition;

		Assert.AreEqual(SpeechTaskHint.Unspecified, _engine.LastOptions!.TaskHint);
		CollectionAssert.AreEqual(new[] { "yes", "no", "maybe later" }, _engine.LastOptions.ContextualStrings.ToArray());
		Assert.AreEqual("maybe later", result.Text);
		Assert.AreSame(answers, result.Constraint);
		Assert.AreEqual(SpeechRecognitionConfidence.High, result.Confidence);
		Assert.AreEqual(1, result.GetAlternates(5).Count);
	}

	[TestMethod]
	public async Task When_List_Constraint_Not_Matched_Then_Rejected()
	{
		_sut.Constraints.Add(new SpeechRecognitionListConstraint(new[] { "yes", "no" }));
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.Complete("Something else");

		var result = await recognition;

		Assert.AreEqual(SpeechRecognitionResultStatus.Success, result.Status);
		Assert.AreEqual(SpeechRecognitionConfidence.Rejected, result.Confidence);
		Assert.AreEqual(string.Empty, result.Text);
	}

	[TestMethod]
	public async Task When_List_Constraint_Disabled_Then_Dictation_Is_Used()
	{
		_sut.Constraints.Add(new SpeechRecognitionListConstraint(new[] { "yes", "no" }) { IsEnabled = false });
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.Complete("Something else");

		Assert.AreEqual(SpeechTaskHint.Dictation, _engine.LastOptions!.TaskHint);
		Assert.AreEqual("Something else", (await recognition).Text);
	}

	[TestMethod]
	[DataRow(SpeechRecognitionScenario.Dictation, (int)SpeechTaskHint.Dictation)]
	[DataRow(SpeechRecognitionScenario.WebSearch, (int)SpeechTaskHint.Search)]
	[DataRow(SpeechRecognitionScenario.FormFilling, (int)SpeechTaskHint.Unspecified)]
	public async Task When_Topic_Constraint_Then_Task_Hint_Matches(SpeechRecognitionScenario scenario, int expectedHint)
	{
		var topic = new SpeechRecognitionTopicConstraint(scenario, "hint");
		_sut.Constraints.Add(topic);
		var recognition = _sut.RecognizeAsync().AsTask();
		_engine.RaiseSpeechDetected();
		_engine.Complete("Weather in Prague");

		var result = await recognition;

		Assert.AreEqual((SpeechTaskHint)expectedHint, _engine.LastOptions!.TaskHint);
		Assert.AreSame(topic, result.Constraint);
		Assert.AreEqual(SpeechRecognitionConstraintType.Topic, topic.Type);
	}

	[TestMethod]
	public async Task When_Unsupported_Constraint_Then_GrammarCompilationFailure()
	{
		_sut.Constraints.Add(new UnsupportedConstraint());

		Assert.AreEqual(SpeechRecognitionResultStatus.GrammarCompilationFailure, (await _sut.CompileConstraintsAsync().AsTask()).Status);
		Assert.AreEqual(SpeechRecognitionResultStatus.GrammarCompilationFailure, (await _sut.RecognizeAsync().AsTask()).Status);
		Assert.IsFalse(_engine.Calls.Contains("Begin"));
	}

	[TestMethod]
	public async Task When_Timeout_Elapses_On_Another_Thread_Then_Events_Use_The_Starting_Context()
	{
		var context = new QueuedSynchronizationContext();
		var original = SynchronizationContext.Current;
		Task<SpeechRecognitionResult> recognition;
		SynchronizationContext.SetSynchronizationContext(context);
		try
		{
			recognition = _sut.RecognizeAsync().AsTask();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(original);
		}

		_time.Advance(TimeSpan.FromSeconds(5));
		Assert.IsFalse(_engine.Calls.Contains("Cancel"), "The timeout must be handled on the context that started the recognition.");

		context.RunPending();
		Assert.IsTrue(_engine.Calls.Contains("Cancel"));

		_engine.CompleteCanceled();
		while (!recognition.IsCompleted)
		{
			context.RunPending();
			await Task.Yield();
		}

		Assert.AreEqual(SpeechRecognitionResultStatus.TimeoutExceeded, (await recognition).Status);
	}

	[TestMethod]
	public void When_Language_Tags_Then_Invalid_Tags_Are_Skipped()
	{
		var languages = SpeechRecognizer.GetSupportedLanguages(new[] { "en-US", "not a language!", "fr-FR" });

		CollectionAssert.AreEqual(new[] { "en-US", "fr-FR" }, languages.Select(language => language.LanguageTag).ToArray());
	}

	[TestMethod]
	public void When_No_System_Speech_Language_Then_Falls_Back_To_UI_Culture()
	{
		Assert.AreEqual("fr-FR", SpeechRecognizer.GetSystemSpeechLanguage("fr-FR").LanguageTag);
		Assert.IsFalse(string.IsNullOrEmpty(SpeechRecognizer.GetSystemSpeechLanguage(null).LanguageTag));
	}

	[TestMethod]
	public async Task When_Setting_System_Speech_Language_Then_Not_Supported()
		=> Assert.IsFalse(await SpeechRecognizer.TrySetSystemSpeechLanguageAsync(new Language("fr-FR")).AsTask());

	[TestMethod]
	public async Task When_Continuous_Then_Each_Phrase_Raises_ResultGenerated()
	{
		var session = _sut.ContinuousRecognitionSession;
		var results = new List<string>();
		var completions = new List<SpeechRecognitionResultStatus>();
		session.ResultGenerated += (_, args) => results.Add(args.Result.Text);
		session.Completed += (_, args) => completions.Add(args.Status);

		await session.StartAsync().AsTask();
		Assert.AreEqual(SpeechRecognizerState.Capturing, _sut.State);

		SpeakAndPause("first");
		Assert.AreEqual(SpeechRecognizerState.SoundEnded, _sut.State);
		_engine.Complete("First phrase");
		CollectionAssert.AreEqual(new[] { "Begin", "End", "Begin" }, _engine.Calls);
		Assert.AreEqual(SpeechRecognizerState.Capturing, _sut.State);

		SpeakAndPause("second");
		_engine.Complete("Second phrase");

		CollectionAssert.AreEqual(new[] { "First phrase", "Second phrase" }, results);
		Assert.AreEqual(0, completions.Count);
		Assert.IsFalse(_engine.Calls.Contains("StopAudio"));
		Assert.IsFalse(_states.Contains(SpeechRecognizerState.Processing));

		var stop = session.StopAsync().AsTask();
		Assert.AreEqual("Cancel", _engine.Calls.Last());
		_engine.CompleteCanceled();
		await stop;

		CollectionAssert.AreEqual(new[] { SpeechRecognitionResultStatus.Success }, completions);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
		Assert.AreEqual("StopAudio", _engine.Calls.Last());
	}

	[TestMethod]
	public async Task When_Continuous_Stopped_During_Phrase_Then_Phrase_Is_Delivered()
	{
		var session = _sut.ContinuousRecognitionSession;
		var results = new List<string>();
		session.ResultGenerated += (_, args) => results.Add(args.Result.Text);
		await session.StartAsync().AsTask();
		_engine.RaiseSpeechDetected();

		var stop = session.StopAsync().AsTask();
		Assert.AreEqual("End", _engine.Calls.Last());
		Assert.AreEqual(SpeechRecognizerState.Processing, _sut.State);

		_engine.Complete("Last words");
		await stop;

		CollectionAssert.AreEqual(new[] { "Last words" }, results);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Continuous_Canceled_Then_Pending_Phrase_Is_Discarded()
	{
		var session = _sut.ContinuousRecognitionSession;
		var results = new List<string>();
		var completions = new List<SpeechRecognitionResultStatus>();
		session.ResultGenerated += (_, args) => results.Add(args.Result.Text);
		session.Completed += (_, args) => completions.Add(args.Status);
		await session.StartAsync().AsTask();
		_engine.RaiseSpeechDetected();

		var cancel = session.CancelAsync().AsTask();
		Assert.AreEqual("Cancel", _engine.Calls.Last());
		_engine.CompleteCanceled();
		await cancel;

		Assert.AreEqual(0, results.Count);
		CollectionAssert.AreEqual(new[] { SpeechRecognitionResultStatus.UserCanceled }, completions);
	}

	[TestMethod]
	public async Task When_Continuous_Then_Speech_Postpones_AutoStop_Instead_Of_Initial_Silence()
	{
		var session = _sut.ContinuousRecognitionSession;
		Assert.AreEqual(TimeSpan.FromSeconds(20), session.AutoStopSilenceTimeout);
		session.AutoStopSilenceTimeout = TimeSpan.FromSeconds(10);
		await session.StartAsync().AsTask();

		_time.Advance(TimeSpan.FromSeconds(6));
		Assert.IsFalse(_engine.Calls.Contains("Cancel"), "The initial silence timeout doesn't apply to continuous recognition.");

		var completions = new List<SpeechRecognitionResultStatus>();
		session.Completed += (_, args) => completions.Add(args.Status);
		_engine.RaiseHypothesis("still here");
		_time.Advance(TimeSpan.FromSeconds(0.15));
		Assert.AreEqual("End", _engine.Calls.Last());
		_engine.Complete("Still here");

		_time.Advance(TimeSpan.FromSeconds(9.75));
		Assert.IsFalse(_engine.Calls.Contains("Cancel"));

		_time.Advance(TimeSpan.FromSeconds(0.1));
		Assert.AreEqual("Cancel", _engine.Calls.Last());
		_engine.CompleteCanceled();

		CollectionAssert.AreEqual(new[] { SpeechRecognitionResultStatus.TimeoutExceeded }, completions);
	}

	[TestMethod]
	public async Task When_Continuous_Silent_Then_Session_Stops()
	{
		var session = _sut.ContinuousRecognitionSession;
		var completions = new List<SpeechRecognitionResultStatus>();
		session.Completed += (_, args) => completions.Add(args.Status);
		session.AutoStopSilenceTimeout = TimeSpan.FromSeconds(2);
		await session.StartAsync().AsTask();

		_time.Advance(TimeSpan.FromSeconds(2));
		Assert.AreEqual("Cancel", _engine.Calls.Last());
		_engine.CompleteCanceled();

		CollectionAssert.AreEqual(new[] { SpeechRecognitionResultStatus.TimeoutExceeded }, completions);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Continuous_Paused_Then_Resume_Starts_New_Phrase()
	{
		var session = _sut.ContinuousRecognitionSession;
		var results = new List<string>();
		session.ResultGenerated += (_, args) => results.Add(args.Result.Text);
		await session.StartAsync().AsTask();
		_engine.RaiseSpeechDetected();

		var pause = session.PauseAsync().AsTask();
		Assert.AreEqual("End", _engine.Calls.Last());
		_engine.Complete("Before pause");
		await pause;

		Assert.AreEqual(SpeechRecognizerState.Paused, _sut.State);
		CollectionAssert.AreEqual(new[] { "Before pause" }, results);
		Assert.IsFalse(_engine.Calls.Contains("StopAudio"), "Audio keeps being captured while paused.");

		_sut.Constraints.Add(new SpeechRecognitionListConstraint(new[] { "next" }));
		session.Resume();

		Assert.AreEqual(SpeechRecognizerState.Capturing, _sut.State);
		Assert.AreEqual("Begin", _engine.Calls.Last());
		CollectionAssert.AreEqual(new[] { "next" }, _engine.LastOptions!.ContextualStrings.ToArray());
	}

	[TestMethod]
	public async Task When_PauseOnRecognition_Then_Pauses_After_Each_Phrase()
	{
		var session = _sut.ContinuousRecognitionSession;
		await session.StartAsync(SpeechContinuousRecognitionMode.PauseOnRecognition).AsTask();

		SpeakAndPause("phrase");
		_engine.Complete("Phrase");

		Assert.AreEqual(SpeechRecognizerState.Paused, _sut.State);
		Assert.AreEqual(1, _engine.Calls.Count(call => call == "Begin"));
	}

	[TestMethod]
	public async Task When_Continuous_Engine_Fails_Then_Completed_With_Status()
	{
		var session = _sut.ContinuousRecognitionSession;
		var completions = new List<SpeechRecognitionResultStatus>();
		session.Completed += (_, args) => completions.Add(args.Status);
		await session.StartAsync().AsTask();

		_engine.CompleteError(SpeechRecognitionResultStatus.MicrophoneUnavailable);

		CollectionAssert.AreEqual(new[] { SpeechRecognitionResultStatus.MicrophoneUnavailable }, completions);
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);
	}

	[TestMethod]
	public async Task When_Continuous_Running_Then_RecognizeAsync_Throws()
	{
		await _sut.ContinuousRecognitionSession.StartAsync().AsTask();

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _sut.RecognizeAsync().AsTask());
	}

	[TestMethod]
	public async Task When_Audio_Clips_Then_TooLoud_Is_Reported_Once()
	{
		var problems = new List<SpeechRecognitionAudioProblem>();
		_sut.RecognitionQualityDegrading += (_, args) => problems.Add(args.Problem);
		var recognition = _sut.RecognizeAsync().AsTask();

		for (var i = 0; i < 6; i++)
		{
			_engine.RaiseAudioLevel(-3f, peakDecibels: 0f);
		}

		CollectionAssert.AreEqual(new[] { SpeechRecognitionAudioProblem.TooLoud }, problems);
		_engine.CompleteCanceled();
		await recognition;
	}

	[TestMethod]
	public async Task When_Microphone_Silent_Then_NoSignal_Is_Reported()
	{
		var problems = new List<SpeechRecognitionAudioProblem>();
		_sut.RecognitionQualityDegrading += (_, args) => problems.Add(args.Problem);
		var recognition = _sut.RecognizeAsync().AsTask();

		for (var i = 0; i < 20; i++)
		{
			_engine.RaiseAudioLevel(-160f, peakDecibels: -160f);
		}

		CollectionAssert.AreEqual(new[] { SpeechRecognitionAudioProblem.NoSignal }, problems);
		_engine.CompleteCanceled();
		await recognition;
	}

	[TestMethod]
	public async Task When_Speech_Is_Faint_Then_TooQuiet_Is_Reported()
	{
		var problems = new List<SpeechRecognitionAudioProblem>();
		_sut.RecognitionQualityDegrading += (_, args) => problems.Add(args.Problem);
		var recognition = _sut.RecognizeAsync().AsTask();

		_engine.RaiseAudioLevel(-58f, peakDecibels: -50f);
		_engine.RaiseSpeechDetected();
		Assert.AreEqual(0, problems.Count);

		_time.Advance(TimeSpan.FromMilliseconds(150));
		CollectionAssert.AreEqual(new[] { SpeechRecognitionAudioProblem.TooQuiet }, problems);
		_engine.CompleteCanceled();
		await recognition;
	}

	[TestMethod]
	public async Task When_Speech_Detected_At_Onset_Then_TooQuiet_Is_Not_Reported()
	{
		var problems = new List<SpeechRecognitionAudioProblem>();
		_sut.RecognitionQualityDegrading += (_, args) => problems.Add(args.Problem);
		var recognition = _sut.RecognizeAsync().AsTask();

		_engine.RaiseAudioLevel(-58f, peakDecibels: -50f);
		_engine.RaiseSpeechDetected();
		_engine.RaiseAudioLevel(-25f, peakDecibels: -12f);
		_time.Advance(TimeSpan.FromMilliseconds(150));

		Assert.AreEqual("End", _engine.Calls.Last());
		Assert.AreEqual(0, problems.Count);
		_engine.Complete("Hello");
		await recognition;
	}

	[TestMethod]
	public async Task When_Background_Is_Loud_Then_TooNoisy_Is_Reported()
	{
		var problems = new List<SpeechRecognitionAudioProblem>();
		_sut.RecognitionQualityDegrading += (_, args) => problems.Add(args.Problem);
		var recognition = _sut.RecognizeAsync().AsTask();

		for (var i = 0; i < 12; i++)
		{
			_engine.RaiseAudioLevel(-25f, peakDecibels: -15f);
		}

		CollectionAssert.AreEqual(new[] { SpeechRecognitionAudioProblem.TooNoisy }, problems);
		_engine.CompleteCanceled();
		await recognition;
	}

	[TestMethod]
	[DataRow(false, SpeechRecognitionResultStatus.Success)]
	[DataRow(true, SpeechRecognitionResultStatus.UserCanceled)]
	public async Task When_Continuous_Stopped_During_Permission_Prompt_Then_Recognition_Does_Not_Start(bool cancel, SpeechRecognitionResultStatus expected)
	{
		var authorization = new TaskCompletionSource<SpeechRecognizerAuthorization>();
		_engine.PendingAuthorization = authorization;
		var session = _sut.ContinuousRecognitionSession;
		var completions = new List<SpeechRecognitionResultStatus>();
		session.Completed += (_, args) => completions.Add(args.Status);
		var start = session.StartAsync().AsTask();

		await (cancel ? session.CancelAsync() : session.StopAsync()).AsTask();
		authorization.SetResult(SpeechRecognizerAuthorization.Authorized);
		await start;

		CollectionAssert.AreEqual(new[] { expected }, completions);
		Assert.IsFalse(_engine.Calls.Contains("Begin"));
		Assert.AreEqual(SpeechRecognizerState.Idle, _sut.State);

		await session.StartAsync().AsTask();
		Assert.AreEqual("Begin", _engine.Calls.Last(), "A new session can start after the stopped one.");
	}

	[TestMethod]
	public async Task When_Continuous_Paused_During_Permission_Prompt_Then_Stays_Paused()
	{
		var authorization = new TaskCompletionSource<SpeechRecognizerAuthorization>();
		_engine.PendingAuthorization = authorization;
		var session = _sut.ContinuousRecognitionSession;
		var start = session.StartAsync().AsTask();

		await session.PauseAsync().AsTask();
		authorization.SetResult(SpeechRecognizerAuthorization.Authorized);
		await start;

		Assert.AreEqual(SpeechRecognizerState.Paused, _sut.State);
		Assert.IsFalse(_engine.Calls.Contains("Begin"));

		session.Resume();
		Assert.AreEqual("Begin", _engine.Calls.Last());
		Assert.AreEqual(SpeechRecognizerState.Capturing, _sut.State);
	}

	[TestMethod]
	public async Task When_Continuous_Resumed_During_Permission_Prompt_Then_Starts_Once_Allowed()
	{
		var authorization = new TaskCompletionSource<SpeechRecognizerAuthorization>();
		_engine.PendingAuthorization = authorization;
		var session = _sut.ContinuousRecognitionSession;
		var start = session.StartAsync().AsTask();

		await session.PauseAsync().AsTask();
		session.Resume();
		Assert.IsFalse(_engine.Calls.Contains("Begin"), "Recognition can't start before permission is granted.");

		authorization.SetResult(SpeechRecognizerAuthorization.Authorized);
		await start;

		Assert.AreEqual(1, _engine.Calls.Count(call => call == "Begin"));
		Assert.AreEqual(SpeechRecognizerState.Capturing, _sut.State);
	}

	private void CreateRecognizer(bool languageSupported = true)
	{
		_engine = new FakeSpeechRecognizerExtension { IsLanguageSupported = languageSupported };
		_time = new ManualTimeProvider();
		_sut = new SpeechRecognizer(new Language("en-US"), _engine, _time);
		_states = new();
		_hypotheses = new();
		_sut.StateChanged += (_, args) => _states.Add(args.State);
		_sut.HypothesisGenerated += (_, args) => _hypotheses.Add(args.Hypothesis.Text);
	}

	private void SpeakAndPause(string hypothesis)
	{
		_engine.RaiseAudioLevel(FakeSpeechRecognizerExtension.LoudDecibels);
		_engine.RaiseSpeechDetected();
		_engine.RaiseHypothesis(hypothesis);

		for (var i = 0; i < 4; i++)
		{
			_time.Advance(TimeSpan.FromMilliseconds(50));
			_engine.RaiseAudioLevel(FakeSpeechRecognizerExtension.QuietDecibels);
		}
	}
}

internal sealed class UnsupportedConstraint : ISpeechRecognitionConstraint
{
	public bool IsEnabled { get; set; } = true;

	public SpeechRecognitionConstraintProbability Probability { get; set; }

	public string Tag { get; set; } = string.Empty;

	public SpeechRecognitionConstraintType Type => SpeechRecognitionConstraintType.Grammar;
}

internal sealed class FakeSpeechRecognizerExtension : ISpeechRecognizerExtension
{
	public const float LoudDecibels = -20f;
	public const float QuietDecibels = -70f;

	public List<string> Calls { get; } = new();

	public SpeechUtteranceOptions? LastOptions { get; private set; }

	public bool IsLanguageSupported { get; set; } = true;

	public SpeechRecognizerAuthorization Authorization { get; set; } = SpeechRecognizerAuthorization.Authorized;

	public IReadOnlyList<string> SupportedLanguageTags { get; set; } = new[] { "en-US", "fr-FR" };

	public string? SystemLanguageTag { get; set; } = "en-US";

	public bool IsDisposed { get; private set; }

	public event EventHandler<string>? HypothesisGenerated;
	public event EventHandler? SpeechDetected;
	public event EventHandler? AudioCaptureEnded;
	public event EventHandler<SpeechAudioLevel>? AudioLevelChanged;
	public event EventHandler<SpeechUtteranceOutcome>? UtteranceCompleted;

	public bool Initialize(Language language) => IsLanguageSupported;

	public TaskCompletionSource<SpeechRecognizerAuthorization>? PendingAuthorization { get; set; }

	public Task<SpeechRecognizerAuthorization> RequestAuthorizationAsync() => PendingAuthorization?.Task ?? Task.FromResult(Authorization);

	public void BeginUtterance(SpeechUtteranceOptions options)
	{
		LastOptions = options;
		Calls.Add("Begin");
	}

	public void EndUtterance() => Calls.Add("End");

	public void CancelUtterance() => Calls.Add("Cancel");

	public void StopAudio() => Calls.Add("StopAudio");

	public IReadOnlyList<string> GetSupportedLanguageTags() => SupportedLanguageTags;

	public string? GetSystemLanguageTag() => SystemLanguageTag;

	public void Dispose() => IsDisposed = true;

	public void RaiseSpeechDetected() => SpeechDetected?.Invoke(this, EventArgs.Empty);

	public void RaiseHypothesis(string text) => HypothesisGenerated?.Invoke(this, text);

	public void RaiseAudioCaptureEnded() => AudioCaptureEnded?.Invoke(this, EventArgs.Empty);

	public void RaiseAudioLevel(float rmsDecibels, float peakDecibels = -10f)
		=> AudioLevelChanged?.Invoke(this, new SpeechAudioLevel(rmsDecibels, peakDecibels));

	public void Complete(params string[] texts)
		=> Complete(texts.Select(text => new SpeechTranscription(text, 0.9, TimeSpan.Zero, TimeSpan.FromSeconds(1))).ToArray());

	public void Complete(SpeechTranscription[] transcriptions, DateTimeOffset? audioStartTime = null)
		=> UtteranceCompleted?.Invoke(this, new SpeechUtteranceOutcome(SpeechUtteranceOutcomeKind.Success, transcriptions, audioStartTime));

	public void CompleteCanceled()
		=> UtteranceCompleted?.Invoke(this, new SpeechUtteranceOutcome(SpeechUtteranceOutcomeKind.Canceled, Array.Empty<SpeechTranscription>()));

	public void CompleteError(SpeechRecognitionResultStatus status)
		=> UtteranceCompleted?.Invoke(this, new SpeechUtteranceOutcome(SpeechUtteranceOutcomeKind.Error, Array.Empty<SpeechTranscription>(), ErrorStatus: status));
}

/// <summary>A synchronization context that only runs posted work when <see cref="RunPending"/> is called.</summary>
internal sealed class QueuedSynchronizationContext : SynchronizationContext
{
	private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = new();

	public override void Post(SendOrPostCallback d, object? state) => _pending.Enqueue((d, state));

	public void RunPending()
	{
		var previous = Current;
		SetSynchronizationContext(this);
		try
		{
			while (_pending.TryDequeue(out var work))
			{
				work.Callback(work.State);
			}
		}
		finally
		{
			SetSynchronizationContext(previous);
		}
	}
}

/// <summary>A <see cref="TimeProvider"/> whose timers only fire when <see cref="Advance"/> is called.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
	private readonly List<ManualTimer> _timers = new();
	private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

	public override DateTimeOffset GetUtcNow() => _now;

	public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		var timer = new ManualTimer(this, callback, state);
		_timers.Add(timer);
		timer.Change(dueTime, period);
		return timer;
	}

	public void Advance(TimeSpan delta)
	{
		var target = _now + delta;
		while (_timers.Where(t => t.DueTime <= target).OrderBy(t => t.DueTime).FirstOrDefault() is { } timer)
		{
			_now = timer.DueTime!.Value;
			timer.DueTime = null;
			timer.Fire();
		}

		_now = target;
	}

	private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
	{
		public DateTimeOffset? DueTime { get; set; }

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			DueTime = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime;
			return true;
		}

		public void Fire() => callback(state);

		public void Dispose()
		{
			DueTime = null;
			owner._timers.Remove(this);
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
