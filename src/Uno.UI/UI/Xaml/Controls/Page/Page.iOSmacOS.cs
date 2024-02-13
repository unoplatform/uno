using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Page
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Page()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}

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

	partial void UpdateBorder() => _borderRenderer.Update();
}
