#if __ANDROID__
using System;
using Windows.Foundation;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizer : IDisposable
	{
		private void InitializeSpeechRecognizer()
		{
		}

		public IAsyncOperation<SpeechRecognitionResult> RecognizeAsync()
		{
			return null;
		}

		public IAsyncAction StopRecognitionAsync()
		{
			return null;
		}

		public void Dispose()
		{
		}
	}
}
#endif
