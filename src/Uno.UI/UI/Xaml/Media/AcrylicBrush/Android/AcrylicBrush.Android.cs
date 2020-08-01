using System;
using Uno.Disposables;
using Uno.UI.Controls;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
    {
        internal IDisposable Apply(BindableView owner)
		{			
			SetAcrylicBlur(owner);
			UpdateProperties();
			return new CompositeDisposable();
		}
    }
}
