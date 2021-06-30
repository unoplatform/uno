using Windows.UI.Text;
using Pango;

namespace Uno.UI.Runtime.Skia.GTK.UI.Text
{
	public static class FontWeightExtensions
	{
		/// <summary>
		/// Based on <see cref="https://github.com/GNOME/pango/blob/master/pango/pango-font.h" />
		/// and <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.ui.text.fontweights?view=winrt-19041" />.
		/// </summary>
		/// <param name="weight">Font weight.</param>
		/// <returns>Pango weight.</returns>
		public static Weight ToPangoWeight(this FontWeight weight) => (Weight)weight.Weight;
	}
}
