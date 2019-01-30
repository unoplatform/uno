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
	}
}
