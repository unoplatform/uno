using Android.Content;
using Android.Graphics;
using Uno.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media
{
	public class EmptyBlurImpl : IBlurImpl
	{
		public bool Prepare(Context context, Bitmap buffer, float radius) => false;

		public void Release()
		{
		}

		public void Blur(Bitmap input, Bitmap output)
		{
		}
	}
}
