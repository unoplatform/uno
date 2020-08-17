#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.Media.SpeechRecognition
{
	public  partial class SpeechRecognitionResult
	{
		public double RawConfidence { get; set; }

		public string Text { get; set; }

		internal IReadOnlyList<SpeechRecognitionResult> Alternates { get; set; }

		public IReadOnlyList<SpeechRecognitionResult> GetAlternates(uint maxAlternates) => Alternates?.Take((int)maxAlternates).ToList();
	}
}
#endif
