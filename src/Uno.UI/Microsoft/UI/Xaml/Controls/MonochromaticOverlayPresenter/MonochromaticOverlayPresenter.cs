using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class MonochromaticOverlayPresenter : Grid
	{
		private static bool _warned;

		//private CompositionEffectFactory _effectFactory = null;
		//private Color _replacementColor;
		private bool _needsBrushUpdate;

		public MonochromaticOverlayPresenter()
		{
			SizeChanged += (s, e) => InvalidateBrush();
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;

			InvalidateBrush();
		}

		private void InvalidateBrush()
		{
			// Delay brush updates until Tick to coalesce changes and avoid rebuilding the effect when we don't need to.
			if (!_needsBrushUpdate)
			{
				_needsBrushUpdate = true;
				SharedHelpers.QueueCallbackForCompositionRendering(() =>
				{
					{
						UpdateBrush();
					}
					_needsBrushUpdate = false;
				});
			}
		}

		private void UpdateBrush()
		{
			// TODO Uno: Required Composition APIs are not implemented yet, warn the user.
			if (!_warned)
			{
				_warned = true;
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Log(
						LogLevel.Warning,
						"MonochromaticOverlayPresenter is not yet supported in Uno Platform and currently does not display any content");
				}
			}
		}
	}
}
