using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;
using Windows.UI.Input;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget : ICompositionTarget
{
	private Visual _root;
	private double _rasterizationScale;
	private EventHandler _rasterizationScaleChanged;

	internal CompositionTarget(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;
		var xamlRoot = contentRoot.GetOrCreateXamlRoot();
		_rasterizationScale = xamlRoot.RasterizationScale;
		xamlRoot.Changed += XamlRoot_Changed;
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

	double ICompositionTarget.RasterizationScale => _rasterizationScale;

	public static Compositor GetCompositorForCurrentThread() => Compositor.GetSharedCompositor();

	void ICompositionTarget.TryRedirectForManipulation(PointerPoint pointerPoint, InteractionTracker tracker)
	{
#if UNO_HAS_MANAGED_POINTERS // TODO: Support more platforms
		ContentRoot.InputManager.Pointers.RedirectPointer(pointerPoint, tracker);
#endif
	}

	private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		if (ContentRoot.XamlRoot.RasterizationScale != _rasterizationScale)
		{
			_rasterizationScale = ContentRoot.XamlRoot.RasterizationScale;
			_rasterizationScaleChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
