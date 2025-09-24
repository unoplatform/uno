using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Uno.UI.RemoteControl.Helpers
{
	/// <summary>
	/// Console banner rendering utilities.
	/// </summary>
	public static class BannerHelper
	{
		public enum ClipMode
		{
			/// <summary>
			/// Clip at the end of the value and add an ellipsis at the end (keeps the beginning).
			/// </summary>
			End,
			/// <summary>
			/// Clip at the beginning of the value and add an ellipsis at the beginning (keeps the end).
			/// </summary>
			Start
		}

		public sealed record BannerEntry(string Key, string? Value, ClipMode Clip = ClipMode.End)
		{
			public static implicit operator BannerEntry((string Key, string Value, ClipMode Clip) t) => new(t.Key, t.Value, t.Clip);
			public static implicit operator BannerEntry((string Key, string Value) t) => new(t.Key, t.Value);
		}

		public static void Write(string title, IReadOnlyDictionary<string, string> dictionary, int maxInnerWidth = 118, TextWriter? output = null)
		{
			ArgumentNullException.ThrowIfNull(title);
			ArgumentNullException.ThrowIfNull(dictionary);
			Write(title, [.. dictionary.Select(kvp => new BannerEntry(kvp.Key, kvp.Value))], maxInnerWidth, output);
		}

		/// <summary>
		/// Writes a banner to the console with a centered title and key/value lines.
		/// </summary>
		/// <param name="title">The title displayed at the top of the banner.</param>
		/// <param name="entries">List of entries to display, each with a clipping strategy.</param>
		/// <param name="innerWidth">
		/// The width of the content between the vertical borders. Defaults to 118 to match existing formatting.
		/// Total line width will be innerWidth + 2.
		/// </param>
		/// <param name="output">The writer to use for output. Defaults to <see cref="Console.Out"/>.</param>
		public static void Write(string title, IReadOnlyCollection<BannerEntry> entries, int maxInnerWidth = 118, TextWriter? output = null)
		{
			ArgumentNullException.ThrowIfNull(title);
			ArgumentNullException.ThrowIfNull(entries);
			var writer = new StringWriter(CultureInfo.InvariantCulture);

			if (maxInnerWidth < 20)
			{
				maxInnerWidth = 20;
			}

			// Determine raw maximum key width across entries
			var rawKeyWidth = 6;
			var longestValue = 0;
			if (entries.Count != 0)
			{
				foreach (var e in entries)
				{
					var keyLen = (e.Key ?? string.Empty).Length;
					if (keyLen > rawKeyWidth)
					{
						rawKeyWidth = keyLen;
					}
					var valueLen = (e.Value ?? string.Empty).Length;
					if (valueLen > longestValue)
					{
						longestValue = valueLen;
					}
				}
			}

			// Enforce that key width never exceeds 50% of the maximum inner width (while keeping a minimum of 8)
			var maxKeyWidth = Math.Max(8, maxInnerWidth / 2);
			var keyWidth = rawKeyWidth > maxKeyWidth ? maxKeyWidth : rawKeyWidth;

			// Compute required inner width so that the longest key/value fits, but do not exceed maxInnerWidth
			var sep = " : ";
			var requiredContentWidth = Math.Max(
				title.Length,
				1 /* leading space */ + keyWidth + sep.Length + longestValue + 1 /* trailing space */
			);
			var innerWidth = requiredContentWidth < maxInnerWidth ? requiredContentWidth : maxInnerWidth;

			// Box lines
			writer.WriteLine($"+{new string('=', innerWidth)}+");

			// Title centered
			var titleTrim = title.Length > innerWidth ? Clip(title, innerWidth, ClipMode.End) : title;
			var leftPad = (innerWidth - titleTrim.Length) / 2;
			var rightPad = innerWidth - titleTrim.Length - leftPad;
			writer.WriteLine($"|{new string(' ', leftPad)}{titleTrim}{new string(' ', rightPad)}|");

			// Divider below title
			if (entries.Count > 0)
			{
				writer.WriteLine($"+{new string('-', innerWidth)}+");

				// Render entries
				foreach (var e in entries)
				{
					var keyText = e.Key ?? string.Empty;
					if (keyText.Length > keyWidth)
					{
						keyText = Clip(keyText, keyWidth, ClipMode.End);
					}
					var key = keyText.PadRight(keyWidth);
					var valueWidth = innerWidth - 1 /*leading space*/ - keyWidth - sep.Length - 1 /*trailing space*/;
					if (valueWidth < 0) valueWidth = 0;

					var rawValue = e.Value ?? string.Empty;
					var value = rawValue.Length > valueWidth ? Clip(rawValue, valueWidth, e.Clip) : rawValue;
					var padded = value.PadRight(valueWidth);
					writer.WriteLine($"| {key}{sep}{padded} |");
				}
			}

			writer.WriteLine($"+{new string('=', innerWidth)}+");

			// Write to output
			(output ?? Console.Out).Write(writer.ToString());
		}

		private static string Clip(string value, int maxWidth, ClipMode mode)
		{
			if (maxWidth <= 0) return string.Empty;
			if (value.Length <= maxWidth) return value;
			if (maxWidth <= 3)
			{
				// Not enough room for text + ellipsis, just return trimmed ellipsis
				return new string('.', Math.Max(0, maxWidth));
			}

			return mode switch
			{
				ClipMode.Start => string.Concat("...", value.AsSpan(value.Length - (maxWidth - 3))),
				_ => string.Concat(value.AsSpan(0, maxWidth - 3), "..."),
			};
		}
	}
}
