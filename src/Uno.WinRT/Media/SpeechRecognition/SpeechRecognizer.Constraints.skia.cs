#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.Globalization;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognizer
{
	private static IReadOnlyList<Language>? _supportedLanguages;

	public static IReadOnlyList<Language> SupportedTopicLanguages => GetSupportedLanguages();

	// List constraints are recognized by the same engine as topics.
	public static IReadOnlyList<Language> SupportedGrammarLanguages => GetSupportedLanguages();

	public static Language SystemSpeechLanguage
		=> GetSystemSpeechLanguage(UseStaticExtension<string>(extension => extension.GetSystemLanguageTag()));

	public IList<ISpeechRecognitionConstraint> Constraints { get; } = new List<ISpeechRecognitionConstraint>();

	// The speech language of the platform can't be changed by apps.
	public static IAsyncOperation<bool> TrySetSystemSpeechLanguageAsync(Language speechLanguage)
		=> AsyncOperation.FromTask(_ => Task.FromResult(false));

	internal static IReadOnlyList<Language> GetSupportedLanguages(IEnumerable<string> languageTags)
		=> languageTags.Select(TryCreateLanguage).OfType<Language>().ToList();

	internal static Language GetSystemSpeechLanguage(string? languageTag)
		=> TryCreateLanguage(languageTag)
			?? TryCreateLanguage(CultureInfo.CurrentUICulture.Name)
			?? new Language("en-US");

	// The platform's supported languages don't change while the app runs.
	private static IReadOnlyList<Language> GetSupportedLanguages()
		=> _supportedLanguages ??= UseStaticExtension(extension => GetSupportedLanguages(extension.GetSupportedLanguageTags())) ?? Array.Empty<Language>();

	private static T? UseStaticExtension<T>(Func<ISpeechRecognizerExtension, T?> use)
		where T : class
	{
		if (!ApiExtensibility.CreateInstance<ISpeechRecognizerExtension>(typeof(SpeechRecognizer), out var extension))
		{
			return null;
		}

		try
		{
			return use(extension);
		}
		finally
		{
			extension.Dispose();
		}
	}

	private static Language? TryCreateLanguage(string? languageTag)
	{
		if (string.IsNullOrEmpty(languageTag))
		{
			return null;
		}

		try
		{
			return new Language(languageTag);
		}
		catch (CultureNotFoundException)
		{
			return null;
		}
	}

	/// <summary>
	/// The constraints used for one recognition, snapshotted when they are compiled.
	/// </summary>
	private sealed class ConstraintPlan
	{
		// The speech engine ignores contextual phrases past this count.
		private const int MaximumContextualStrings = 100;

		private ConstraintPlan(
			SpeechTaskHint taskHint,
			IReadOnlyList<string> contextualStrings,
			IReadOnlyList<SpeechRecognitionListConstraint> lists,
			SpeechRecognitionTopicConstraint? topic,
			bool hasUnsupportedConstraints)
		{
			Options = new SpeechUtteranceOptions(taskHint, contextualStrings);
			Lists = lists;
			Topic = topic;
			HasUnsupportedConstraints = hasUnsupportedConstraints;
		}

		public SpeechUtteranceOptions Options { get; }

		public IReadOnlyList<SpeechRecognitionListConstraint> Lists { get; }

		public SpeechRecognitionTopicConstraint? Topic { get; }

		public bool HasUnsupportedConstraints { get; }

		public static ConstraintPlan Create(IEnumerable<ISpeechRecognitionConstraint> constraints)
		{
			var lists = new List<SpeechRecognitionListConstraint>();
			SpeechRecognitionTopicConstraint? topic = null;
			var hasUnsupportedConstraints = false;

			foreach (var constraint in constraints)
			{
				switch (constraint)
				{
					case SpeechRecognitionListConstraint list:
						if (list.IsEnabled)
						{
							lists.Add(list);
						}
						break;

					case SpeechRecognitionTopicConstraint topicConstraint:
						if (topicConstraint.IsEnabled)
						{
							topic ??= topicConstraint;
						}
						break;

					default:
						hasUnsupportedConstraints = true;
						break;
				}
			}

			var taskHint = topic?.Scenario switch
			{
				SpeechRecognitionScenario.Dictation => SpeechTaskHint.Dictation,
				SpeechRecognitionScenario.WebSearch => SpeechTaskHint.Search,
				SpeechRecognitionScenario.FormFilling => SpeechTaskHint.Unspecified,
				_ => lists.Count > 0 ? SpeechTaskHint.Unspecified : SpeechTaskHint.Dictation,
			};

			var contextualStrings = lists
				.SelectMany(list => list.Commands)
				.Where(command => !string.IsNullOrWhiteSpace(command))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(MaximumContextualStrings)
				.ToArray();

			return new ConstraintPlan(taskHint, contextualStrings, lists, topic, hasUnsupportedConstraints);
		}

		public (string Command, SpeechRecognitionListConstraint Constraint)? Match(string text)
		{
			var normalizedText = Normalize(text);
			foreach (var list in Lists)
			{
				foreach (var command in list.Commands)
				{
					if (Normalize(command) == normalizedText)
					{
						return (command, list);
					}
				}
			}

			return null;
		}

		// Compares phrases regardless of case, punctuation and spacing.
		private static string Normalize(string text)
		{
			var builder = new StringBuilder(text.Length);
			var pendingSpace = false;
			foreach (var character in text)
			{
				if (char.IsLetterOrDigit(character))
				{
					if (pendingSpace && builder.Length > 0)
					{
						builder.Append(' ');
					}

					builder.Append(char.ToLowerInvariant(character));
					pendingSpace = false;
				}
				else if (char.IsWhiteSpace(character))
				{
					pendingSpace = true;
				}
			}

			return builder.ToString();
		}
	}
}
