using Android.Graphics;

namespace Windows.UI.Xaml.Media
{
	partial interface IGradientBrush
	{
		Shader GetShader(Windows.Foundation.Rect destinationRect);
	}
}
