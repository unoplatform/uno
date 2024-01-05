#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognitionResult
	{
#if __WASM__
		// TODO: Make a breaking change for Android and iOS and make the constructor internal unconditionally.
		internal SpeechRecognitionResult()
		{
		}
#endif

#if __WASM__
		// TODO: Make a breaking change for Android and iOS and make the setters internal unconditionally.
		public double RawConfidence { get; internal set; }
		public string Text { get; internal set; } = null!;
#else
		public double RawConfidence { get; set; }
		public string Text { get; set; }
#endif

		internal IReadOnlyList<SpeechRecognitionResult>? Alternates { get; set; }

		public IReadOnlyList<SpeechRecognitionResult>? GetAlternates(uint maxAlternates) => Alternates?.Take((int)maxAlternates).ToList();
	}
}
#endif
