using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	public partial class FontFamily
	{
		private readonly int _hashCode;

		public FontFamily(string familyName)
		{
			Source = familyName;

			Init(familyName);

			// This instance is immutable, we can cache the hash code.
			_hashCode = familyName.GetHashCode();
		}

		public string Source { get; }

		// Makes introduction of FontFamily a non-breaking change (for now)
		public static implicit operator FontFamily(string familyName) => new FontFamily(familyName);

		public static FontFamily Default { get; } = new FontFamily("Segoe UI");

		public static FontFamily XamlAutoFontFamily => Default;

		public override bool Equals(object obj)
		{
			if (obj is FontFamily fontFamily)
			{
				return Source.Equals(fontFamily.Source, StringComparison.Ordinal);
			}

			return false;
		}

		public override int GetHashCode() => _hashCode;

		public static bool operator ==(FontFamily a, FontFamily b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			return a is not null && a.Equals(b);
		}

		public static bool operator !=(FontFamily a, FontFamily b)
		{
			if (ReferenceEquals(a, b))
			{
				return false;
			}

			return a is null || !a.Equals(b);
		}

		partial void Init(string fontName);
	}
}
