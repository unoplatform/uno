using System;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace Uno.UI.RuntimeTests.Tests.Windows_Media_SpeechRecognition;

[TestClass]
public class Given_SpeechRecognizer
{
	// Exercises WinRT activation, event subscription and teardown of the Win32 backend without a microphone.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Created_Stopped_And_Disposed_Then_No_Exception()
	{
		var recognizer = new SpeechRecognizer(new Language("en-US"));
		recognizer.HypothesisGenerated += (_, _) => { };
		recognizer.StateChanged += (_, _) => { };

		await recognizer.CompileConstraintsAsync();
		await recognizer.StopRecognitionAsync();

		recognizer.Dispose();
	}
}
