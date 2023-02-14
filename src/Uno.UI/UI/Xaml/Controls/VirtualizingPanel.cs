#pragma warning disable 108 // new keyword hiding
using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class VirtualizingPanel : Panel, IVirtualizingPanel
	{
		public VirtualizingPanel()
		{

		}

		public VirtualizingPanelLayout GetLayouter() => GetLayouterCore();

		private protected virtual VirtualizingPanelLayout GetLayouterCore() => throw new NotSupportedException($"This method must be overridden by implementing classes.");
	}
}
