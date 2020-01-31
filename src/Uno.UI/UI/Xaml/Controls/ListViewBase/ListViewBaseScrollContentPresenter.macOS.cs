using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ListViewBaseScrollContentPresenter
	{
		public Rect MakeVisible(UIElement visual, Rect rectangle) => rectangle; //TODO: ? (copied from iOS)
	}
}
