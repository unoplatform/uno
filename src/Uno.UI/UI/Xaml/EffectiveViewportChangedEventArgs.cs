
using Windows.Foundation;

namespace Windows.UI.Xaml
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
