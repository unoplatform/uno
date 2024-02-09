using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class Page
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Page()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}

	private void UpdateBorder() => _borderRenderer.Update();
}
