
using Windows.Foundation;

namespace Microsoft.UI.Xaml
{
	public partial class EffectiveViewportChangedEventArgs
	{
		internal EffectiveViewportChangedEventArgs(Rect effectiveViewport)
		{
			EffectiveViewport = effectiveViewport;
		}

		public Rect EffectiveViewport { get; }
	}
}
