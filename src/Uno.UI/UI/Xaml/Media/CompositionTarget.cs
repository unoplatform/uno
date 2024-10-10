using System.Collections.Generic;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Media;

public partial class CompositionTarget : ICompositionTarget
{
	private Visual _root;

	internal CompositionTarget(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;
	}

	public static Compositor GetCompositorForCurrentThread() => Compositor.GetSharedCompositor();

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

	void ICompositionTarget.TryRedirectForManipulation(PointerPoint pointerPoint, InteractionTracker tracker)
	{
#if UNO_HAS_MANAGED_POINTERS // TODO: Support more platforms
		ContentRoot.InputManager.Pointers.RedirectPointer(pointerPoint, tracker);
#endif
	}
}
