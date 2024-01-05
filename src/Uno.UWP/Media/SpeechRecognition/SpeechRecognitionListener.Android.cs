using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Speech;

namespace Windows.Media.SpeechRecognition
{
	public class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
	{
		public Action? StartOfSpeech { get; set; }

		public Action? EndOfSpeech { get; set; }

		public Action? ReadyForSpeech { get; set; }

		public Action<SpeechRecognizerError>? Error { get; set; }

		public Action<SpeechRecognitionResult>? FinalResults { get; set; }

		public Action<SpeechRecognitionResult>? PartialResults { get; set; }

		public Action<float>? RmsChanged { get; set; }

		public void OnBeginningOfSpeech()
		{
			this.StartOfSpeech?.Invoke();
		}

		public void OnBufferReceived(byte[]? buffer)
		{
		}

		public void OnEndOfSpeech()
		{
			this.EndOfSpeech?.Invoke();
		}

		public void OnError(SpeechRecognizerError error)
		{
			this.Error?.Invoke(error);
		}

		public void OnEvent(int eventType, Bundle? @params)
		{
		}

		public void OnReadyForSpeech(Bundle? @params)
		{
			this.ReadyForSpeech?.Invoke();
		}

		public void OnPartialResults(Bundle? bundle)
		{
			this.SendResults(bundle!, this.PartialResults);
		}

		public void OnResults(Bundle? bundle)
		{
			this.SendResults(bundle!, this.FinalResults);
		}

		public void OnRmsChanged(float rmsdB)
		{
			this.RmsChanged?.Invoke(rmsdB);
		}

		private void SendResults(Bundle bundle, Action<SpeechRecognitionResult>? action)
		{
			var matches = bundle.GetStringArrayList(Android.Speech.SpeechRecognizer.ResultsRecognition);
			var scores = bundle.GetFloatArray(Android.Speech.SpeechRecognizer.ConfidenceScores);

			// If there is nothing to handle
			if (matches == null || matches.Count == 0)
			{
				return;
			}

			// If no score available (partial result), just return the 1st result
			if (scores == null || scores.Length == 0)
			{
				action?.Invoke(new SpeechRecognitionResult()
				{
					Text = matches[0]
				});

				return;
			}

			// Find the best match
			// Note that matches and scores should have the same length (See: https://developer.android.com/reference/android/speech/SpeechRecognizer#CONFIDENCE_SCORES)
			var bestIndex = 0;
			for (var i = 0; i < scores.Length; i++)
			{
				if (scores[bestIndex] < scores[i])
				{
					bestIndex = i;
				}
			}

			// Create the result entity
			var result = new SpeechRecognitionResult()
			{
				Text = matches[bestIndex],
				RawConfidence = scores[bestIndex]
			};

			// Add available alternate texts
			var alternates = new List<SpeechRecognitionResult>();
			for (var i = 0; i < scores.Length; i++)
			{
				if (i == bestIndex)
				{
					continue;
				}

				alternates.Add(new SpeechRecognitionResult()
				{
					Text = matches[i],
					RawConfidence = scores[i]
				});
			}

			result.Alternates = alternates.OrderByDescending(a => a.RawConfidence).ToList();

			action?.Invoke(result);
		}
	}
}
