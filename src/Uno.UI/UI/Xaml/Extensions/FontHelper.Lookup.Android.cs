#nullable enable

using Android.App;
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI;
using System.Linq;
using Android.OS;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.Foundation.Logging;

using Uno.Collections;

namespace Windows.UI.Xaml
{
	internal partial class FontHelper
	{
		private readonly static FontFamilyToTypeFaceDictionary _fontFamilyToTypeFaceDictionary = new FontFamilyToTypeFaceDictionary();

		private class FontFamilyToTypeFaceDictionary
		{
			public class Entry
			{
				private readonly int _hashCode;

				public Entry(string? fontFamily, FontWeight fontWeight, bool italic, FontStretch fontStretch)
				{
					FontFamily = fontFamily;
					FontWeight = fontWeight;
					Italic = italic;
					FontStretch = fontStretch;
					_hashCode = (FontFamily?.GetHashCode() ?? 0) ^ FontWeight.GetHashCode() ^ Italic.GetHashCode() ^ FontStretch.GetHashCode();
				}

				public string? FontFamily { get; }
				public FontWeight FontWeight { get; }
				public bool Italic { get; }
				public FontStretch FontStretch { get; }

				public override bool Equals(object? other)
				{
					if (other is Entry otherEntry)
					{
						return FontFamily == otherEntry.FontFamily
							&& FontWeight == otherEntry.FontWeight
							&& Italic == otherEntry.Italic
							&& FontStretch == otherEntry.FontStretch;
					}

					return false;
				}

				public override int GetHashCode() => _hashCode;
			}

			private readonly HashtableEx _entries = new HashtableEx();

			internal bool TryGetValue(Entry key, out Typeface? result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (Typeface?)value;
					return true;
				}

				result = null;
				return false;
			}

			internal void Add(Entry key, Typeface? typeFace)
				=> _entries.Add(key, typeFace);

			internal void Clear()
				=> _entries.Clear();
		}

	}
}
