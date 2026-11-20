#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText
{
	private static partial class ICU
	{
		// Embedded by the WebAssembly build when it ships the break dictionaries (~2 MB compressed) as a separate
		// app asset instead of inside the embedded data; the resource holds the asset's ms-appx URI.
		private const string DictionaryUriResourceSuffix = "icudt-dict.uri";

		private const int DictionariesEmbedded = 0;
		private const int DictionariesPending = 1;
		private const int DictionariesLoading = 2;
		private const int DictionariesLoaded = 3;

		private static readonly HashSet<IFontCacheUpdateListener> _dictionaryListeners = new();
		private static Uri? _dictionaryUri;
		private static int _dictionaryState;
		private static int _dataGeneration;

		private static void InitOnDemandDictionaries(Assembly dataAssembly)
		{
			var resourceName = dataAssembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(DictionaryUriResourceSuffix, StringComparison.Ordinal));
			if (resourceName is not null && dataAssembly.GetManifestResourceStream(resourceName) is { } stream)
			{
				using var reader = new StreamReader(stream);
				_dictionaryUri = new Uri(reader.ReadToEnd().Trim());
				_dictionaryState = DictionariesPending;
			}
		}

		/// <summary>
		/// Starts loading the on-demand break dictionaries the first time <paramref name="text"/> needs them.
		/// Breaks computed before they arrive are dictionary-less, so <paramref name="listener"/> is invalidated once they do.
		/// </summary>
		public static void EnsureDictionaries(string text, IFontCacheUpdateListener? listener)
		{
			if (Volatile.Read(ref _dictionaryState) is DictionariesEmbedded or DictionariesLoaded || !NeedsDictionary(text))
			{
				return;
			}

			lock (_dictionaryListeners)
			{
				if (_dictionaryState == DictionariesLoaded)
				{
					return;
				}

				if (listener is not null)
				{
					_dictionaryListeners.Add(listener);
				}

				if (_dictionaryState == DictionariesPending)
				{
					_dictionaryState = DictionariesLoading;
					_ = LoadDictionariesAsync(_dictionaryUri!);
				}
			}
		}

		private static async Task LoadDictionariesAsync(Uri uri)
		{
			try
			{
				using var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
				if (stream.CanSeek)
				{
					RegisterCommonData(stream);
				}
				else
				{
					using var buffer = new MemoryStream();
					await stream.CopyToAsync(buffer);
					buffer.Position = 0;
					RegisterCommonData(buffer);
				}
			}
			catch (Exception e)
			{
				typeof(ICU).LogError()?.Error($"Failed to load the ICU break dictionaries from {uri}; Thai, Lao, Khmer, Myanmar and CJK text will break without them.", e);
			}

			IFontCacheUpdateListener[] listeners;
			lock (_dictionaryListeners)
			{
				Interlocked.Increment(ref _dataGeneration);
				_dictionaryState = DictionariesLoaded;
				listeners = _dictionaryListeners.ToArray();
				_dictionaryListeners.Clear();
			}

			NativeDispatcher.Main.Enqueue(() =>
			{
				foreach (var listener in listeners)
				{
					listener.Invalidate();
				}
			});
		}

		// Scripts whose word and line breaks come from the ICU dictionaries (brkitr/*.dict).
		internal static bool NeedsDictionary(string text)
		{
			foreach (var c in text)
			{
				if (c < '฀')
				{
					continue;
				}

				if (c <= '໿' // Thai, Lao
					|| c is >= 'က' and <= '႟' // Myanmar
					|| c is >= 'ក' and <= '៿' or >= '᧠' and <= '᧿' // Khmer
					|| c is >= '　' and <= 'ヿ' or >= 'ㇰ' and <= 'ㇿ' // CJK punctuation, Hiragana, Katakana
					|| c is >= '㐀' and <= '䶿' or >= '一' and <= '鿿' or >= '豈' and <= '﫿' // Han
					|| c is >= 'ꧠ' and <= '꧿' or >= 'ꩠ' and <= 'ꩿ' // Myanmar extensions
					|| c is >= 'ｦ' and <= 'ﾟ' // Halfwidth Katakana
					|| c is >= '\uD840' and <= '\uD8BF') // High surrogates of supplementary Han (planes 2 and 3)
				{
					return true;
				}
			}

			return false;
		}
	}
}
