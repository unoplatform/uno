using System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;
using Windows.UI.Input;
namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget : ICompositionTarget
{
	private Visual _root;

	internal CompositionTarget(ContentRoot contentRoot)
	{
		ContentRoot = contentRoot;
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

	void ICompositionTarget.TryRedirectForManipulation(PointerPoint pointerPoint, List<InteractionTracker> trackers)
		=> ContentRoot.InputManager.RedirectPointer(pointerPoint, trackers);
}
