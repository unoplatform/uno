using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private readonly BorderLayerRenderer _borderRenderer;

	public ContentPresenter()
	{
		_borderRenderer = new BorderLayerRenderer(this);
		InitializeContentPresenter();
	}
}
