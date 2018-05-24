#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
    public partial class FlyoutPresenter : ContentControl
    {
		public FlyoutPresenter()
		{

		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
#endif