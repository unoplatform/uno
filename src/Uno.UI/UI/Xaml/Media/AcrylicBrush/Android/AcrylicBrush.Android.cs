using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Uno.Disposables;
using Uno.UI.Controls;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
    {
		bool ready = false;
        internal IDisposable Apply(BindableView owner)
		{
			if(!(owner is Border))
			{
				throw new InvalidOperationException(
					"AcrylicBrush can currently be applied " +
					"to empty border only on Android");
			}
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
}
