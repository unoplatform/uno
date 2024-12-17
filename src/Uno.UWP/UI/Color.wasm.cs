using System;
using System.Globalization;
using System.Text;

namespace Windows.UI
{
	partial struct Color
	{
		/// <summary>
		/// Get color value in CSS format "rgba(r, g, b, a)"
		/// </summary>
		internal string ToCssString() =>
			"rgba("
			+ R.ToString(CultureInfo.InvariantCulture) + ","
			+ G.ToString(CultureInfo.InvariantCulture) + ","
			+ B.ToString(CultureInfo.InvariantCulture) + ","
			+ (A / 255.0).ToString(CultureInfo.InvariantCulture)
			+ ")";

		/// <summary>
		/// Get color value in "rrggbbaa" as integer value
		/// </summary>
		/// <remarks>
		/// IMPORTANT: We MUST NOT just naively prefix this with # in js without appropriate padding, 
		/// because of 6-digits (00GGBBAA as RRGGBB) and 3-digits (00000BAA as RGB) color notations.
		/// </remarks>
		internal uint ToCssInteger() =>
			// = AARRGGBB << 8 | AA
			// = RRGGBB00 | AA
			// = RRGGBBAA
			AsUInt32() << 8 | A;

		// [JSImport] doesnt allow for `uint` params, so we pass an `int` instead
		internal int ToCssIntegerAsInt() => unchecked((int)ToCssInteger());

		/// <summary>
		/// Get color value in "#rrggbb" or "#rrggbbaa" notation
		/// </summary>
		/// <remarks>
		/// The #rrggbbaa hex color notation requires modern browsers. It is not available in older versions of Internet Explorer.
		/// See also: https://caniuse.com/css-rrggbbaa
		/// </remarks>
		internal string ToHexString()
		{
			var builder = new StringBuilder(10);

			builder.Append('#');
			builder.Append(R.ToString("X2", CultureInfo.InvariantCulture));
			builder.Append(G.ToString("X2", CultureInfo.InvariantCulture));
			builder.Append(B.ToString("X2", CultureInfo.InvariantCulture));

			if (A != 255)
			{
				// Include alpha chanel only when required.
				builder.Append(A.ToString("X2", CultureInfo.InvariantCulture));
			}

			return builder.ToString();
		}
	}
}
