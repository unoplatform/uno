#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Media.SpeechRecognition;

namespace Uno.UI.Tests.Windows_Media_SpeechRecognition;

[UITestClass]
public class Given_WebSpeechErrorMapper
{
	[TestMethod]
	[DataRow("no-speech", SpeechRecognitionResultStatus.TimeoutExceeded)]
	[DataRow("no-match", SpeechRecognitionResultStatus.Success)]
	[DataRow("aborted", SpeechRecognitionResultStatus.UserCanceled)]
	[DataRow("audio-capture", SpeechRecognitionResultStatus.MicrophoneUnavailable)]
	[DataRow("not-allowed", SpeechRecognitionResultStatus.MicrophoneUnavailable)]
	[DataRow("service-not-allowed", SpeechRecognitionResultStatus.MicrophoneUnavailable)]
	[DataRow("network", SpeechRecognitionResultStatus.NetworkFailure)]
	[DataRow("language-not-supported", SpeechRecognitionResultStatus.TopicLanguageNotSupported)]
	[DataRow("bad-grammar", SpeechRecognitionResultStatus.GrammarCompilationFailure)]
	public void When_Known_Error_Then_Mapped(string error, SpeechRecognitionResultStatus expected)
		=> Assert.AreEqual(expected, WebSpeechErrorMapper.ToStatus(error));

	[TestMethod]
	[DataRow("")]
	[DataRow("start-failed: InvalidStateError")]
	[DataRow("NETWORK")]
	public void When_Unknown_Error_Then_Unknown(string error)
		=> Assert.AreEqual(SpeechRecognitionResultStatus.Unknown, WebSpeechErrorMapper.ToStatus(error));
}
