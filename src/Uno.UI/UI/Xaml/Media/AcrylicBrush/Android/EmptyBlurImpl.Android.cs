using Android.Content;
using Android.Graphics;

namespace Uno.UI.Xaml.Media
{
	internal class EmptyBlurImpl : IBlurImpl
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
