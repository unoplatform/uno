#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	public partial class ThicknessHelper
	{
		public static Thickness FromLengths(double left, double top, double right, double bottom)
			=> new Thickness(left, top, right, bottom);

		public static Thickness FromUniformLength(double uniformLength)
			=> new Thickness(uniformLength);
	}
}
