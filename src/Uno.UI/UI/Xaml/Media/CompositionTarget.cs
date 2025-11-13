using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;
using Windows.UI.Input;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget : ICompositionTarget
{
	private Visual _root;
	private double? _rasterizationScale;
	private EventHandler _rasterizationScaleChanged;

	internal CompositionTarget(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;
#if __SKIA__
		_targets.Add(this, null);
		var xamlRoot = ContentRoot.GetOrCreateXamlRoot();
		xamlRoot.Changed += (_, _) => UpdateXamlRootBounds();
		void UpdateXamlRootBounds()
		{
			// _rasterizationScale is asynchronously updated and this can cause problems
			// for the very first frame on app startup where _rasterizationScale is still
			// not set. We read from the DisplayInformation directly instead
			var rasterizationScale = XamlRoot.GetDisplayInformation(xamlRoot).RawPixelsPerViewPixel;
			lock (_xamlRootBoundsGate)
			{
				// Rounding instead of flooring here is specifically necessary on hardware-accelerated Win32, which draws bottom-up,
				// so if there's a mismatch between the actual window height and _xamlRootBounds.Height, the first row of pixels
				// may or not be offset correctly depending on floating point errors
				_xamlRootBounds = xamlRoot.Bounds.Size;
				_xamlRootRasterizationScale = (float)rasterizationScale;
			}
		}
#endif
	}

	event EventHandler ICompositionTarget.RasterizationScaleChanged
	{
		add => _rasterizationScaleChanged += value;
		remove => _rasterizationScaleChanged -= value;
	}

	internal ContentRoot ContentRoot { get; }

	internal Visual Root
	{
		get => _root;
		set
		{
			_root = value;
			_root.CompositionTarget = this;
		}
	}

	double ICompositionTarget.RasterizationScale => _rasterizationScale ?? 1.0;

	public static Compositor GetCompositorForCurrentThread() => Compositor.GetSharedCompositor();

	void ICompositionTarget.TryRedirectForManipulation(PointerPoint pointerPoint, InteractionTracker tracker)
	{
#if UNO_HAS_MANAGED_POINTERS // TODO: Support more platforms
		ContentRoot.InputManager.Pointers.RedirectPointer(pointerPoint, tracker);
#endif
	}

	internal void OnRasterizationScaleChanged(double rasterizationScale)
	{
		_rasterizationScale = rasterizationScale;
		_rasterizationScaleChanged?.Invoke(this, EventArgs.Empty);
	}
}
