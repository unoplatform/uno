using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class Page
{
#if __IOS__
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		UpdateBorder();
	}
#else
	public override void Layout()
	{
		base.Layout();
		UpdateBorder();
	}
#endif
}
