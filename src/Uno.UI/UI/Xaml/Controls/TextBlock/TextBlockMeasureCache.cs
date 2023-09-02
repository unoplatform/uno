#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Foundation;

using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A TextBlock measure cache for non-formatted text.
	/// </summary>
	internal partial class TextBlockMeasureCache
	{
		internal static int MaxMeasureKeyEntries { get; set; } = 500;
		internal static int MaxMeasureSizeKeyEntries { get; set; } = 50;

		private readonly Dictionary<MeasureKey, MeasureEntry> _entries = new Dictionary<MeasureKey, MeasureEntry>(new MeasureKey.Comparer());
		private readonly LinkedList<MeasureKey> _queue = new LinkedList<MeasureKey>();

		public static readonly TextBlockMeasureCache Instance = new TextBlockMeasureCache();

		/// <summary>
		/// Finds a cached measure for the provided <see cref="TextBlock"/> characteristics
		/// given an <paramref name="availableSize"/>.
		/// </summary>
		/// <param name="source">The source</param>
		/// <param name="availableSize">The available size to query</param>
		/// <returns>An optional <see cref="Size"/> if found.</returns>
		public Size? FindMeasuredSize(TextBlock source, Size availableSize)
		{
			if (!FeatureConfiguration.TextBlock.IsMeasureCacheEnabled)
			{
				return null; // Measure cache feature disabled
			}

			var key = new MeasureKey(source); // Extract a key from TextBlock properties
			if (!_entries.TryGetValue(key, out var entry))
			{

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
					this.Log().LogDebug("{0}", $"TextMeasure-cached [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> NOT FOUND.");
				}

				return null; // This key not present in cache
			}

			var measuredSize = entry.FindMeasuredSize(key, availableSize);  // Get cache for specified availableSize, if exists

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
				this.Log().LogDebug("{0}", $"TextMeasure-cached [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
			}

			return measuredSize;
		}

		/// <summary>
		/// Cache a <paramref name="measuredSize"/> for an <paramref name="availableSize"/>, given
		/// the <paramref name="source"/> characteristics.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="availableSize"></param>
		/// <param name="measuredSize"></param>
		public void CacheMeasure(TextBlock source, Size availableSize, Size measuredSize)
		{
			var key = new MeasureKey(source);

			if (_entries.TryGetValue(key, out var entry))
			{
				if (_queue.Count > 1
					&& _queue.Last is not null
					&& !_queue.Last.Value.Equals(key))
				{
					// Move this key as last in the queue for perf
					_queue.Remove(entry.ListNode);
					_queue.AddLast(entry.ListNode);
				}
			}
			else
			{
				Scavenge();
				var node = _queue.AddLast(key);
				_entries[key] = entry = new MeasureEntry(node);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
				this.Log().LogDebug("{0}", $"TextMeasure-new [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
			}

			entry.CacheMeasure(availableSize, measuredSize);
		}

		private void Scavenge()
		{
			while (_queue.Count >= MaxMeasureKeyEntries && _queue.First is not null)
			{
				_entries.Remove(_queue.First.Value);
				_queue.RemoveFirst();
			}
		}
	}
}
