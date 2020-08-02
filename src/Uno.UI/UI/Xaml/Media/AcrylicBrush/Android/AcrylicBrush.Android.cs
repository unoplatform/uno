using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Uno.Disposables;
using Uno.UI.Controls;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
    {
		bool ready = false;
        internal IDisposable Apply(BindableView owner)
		{
			View v = owner;
			
			if (!ready)
			{
				Cleanup(owner);
				SetAcrylicBlur(owner);
				UpdateProperties();
				ready = true;
			}
			return new CompositeDisposable();

		}
	}

	public class AcrylicDrawable : BitmapDrawable
	{
		public AcrylicDrawable(Android.Content.Res.Resources res, Bitmap bitmap)
			: base(res, bitmap)
		{

		}

		public override void Draw(Canvas canvas)
		{
			base.Draw(canvas);
		}
	}
}
