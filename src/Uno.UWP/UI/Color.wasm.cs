using System.Globalization;

namespace Windows.UI
{
	partial struct Color
	{
		/// <summary>
		/// Get color value in CSS format "rgba(r, g, b, a)"
		/// </summary>
		public string ToCssString() => 
			"rgba(" 
			+ R.ToString(CultureInfo.InvariantCulture) + "," 
			+ G.ToString(CultureInfo.InvariantCulture) + "," 
			+ B.ToString(CultureInfo.InvariantCulture) + ","
			+ (A / 255.0).ToString(CultureInfo.InvariantCulture)
			+ ")";

		internal string ToHexString()
		{
			var result = "#";
			if (A != 255)
			{
				// Include alpha chanel only when required.
				result += A.ToString("X2");
			}
			result += $"{R.ToString("X2")}{G.ToString("X2")}{B.ToString("X2")}";
			return result;
		}
	}
}
