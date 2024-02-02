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
		/// Get color value in CSS format "rgba(r, g, b, a)"
		/// </summary>
		internal uint ToCssInteger() =>
			(uint)(R << 24)
			| (uint)(G << 16)
			| (uint)(B << 8)
			| A;

		internal string ToHexString()
		{
			var builder = new StringBuilder(10);

			builder.Append('#');
			builder.Append(R.ToString("X2", CultureInfo.InvariantCulture));
			builder.Append(G.ToString("X2", CultureInfo.InvariantCulture));
			builder.Append(B.ToString("X2", CultureInfo.InvariantCulture));

			if (A != 255)
			{
				// Include alpha channel only when required.
				builder.Append(A.ToString("X2", CultureInfo.InvariantCulture));
			}

			return builder.ToString();
		}
	}
}
