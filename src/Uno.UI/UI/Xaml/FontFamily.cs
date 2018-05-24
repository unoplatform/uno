using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public partial class FontFamily
	{
		private readonly string _source;
		private readonly int _hashCode;

		public FontFamily(string familyName)
		{
			_source = familyName;

			// This instance is immutable, we can cache the hash code.
			_hashCode = _source.GetHashCode();
		}

		public string Source => _source;

		// Makes introduction of FontFamily a non-breaking change (for now)
		public static implicit operator FontFamily(string familyName) => new FontFamily(familyName);

		public static FontFamily Default { get; } = new FontFamily("Segoe UI");

		public override bool Equals(object obj)
		{
			var fontFamily = obj as FontFamily;

			if (fontFamily != null)
			{
				return Source.Equals(fontFamily.Source);
			}

			return false;
		}

		public override int GetHashCode() => _hashCode;
	}
}
