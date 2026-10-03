#if __ANDROID__ || __IOS__ || __WASM__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognitionResult
	{
		internal SpeechRecognitionResult()
		{
		}

		public double RawConfidence { get; internal set; }
		public string Text { get; internal set; }

#if __WASM__
		// Implemented for WebAssembly; the generated partial still provides the NotImplemented stub for
		// the other targets (its #if guard excludes __WASM__).
		public SpeechRecognitionResultStatus Status { get; internal set; }
#endif

		internal IReadOnlyList<SpeechRecognitionResult> Alternates { get; set; }

		public IReadOnlyList<SpeechRecognitionResult> GetAlternates(uint maxAlternates) => Alternates?.Take((int)maxAlternates).ToList();
	}
}
#endif
