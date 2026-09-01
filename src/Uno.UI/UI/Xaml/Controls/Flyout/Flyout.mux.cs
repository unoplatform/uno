using Uno.UI.Extensions;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class Flyout
{
	internal override void PropagateKeyboardAcceleratorEnter(DependencyObject pNamescopeOwner, EnterParams @params)
	{
		base.PropagateKeyboardAcceleratorEnter(pNamescopeOwner, @params);

		var content = Content;
		if (content is not null)
		{
			var visualTree = @params.VisualTree;
#if HAS_UNO // Uno specific: UIElement.EnterImpl nulls VisualTree before calling ContextFlyout.Enter
			// (per WinUI Bug 19548424) to prevent GC retention. Recover it from the owning
			// UIElement (our parent, set just before this Enter call) so KA registration
			// can find the correct ContentRoot via the VisualTree in the EnterParams.
			// Safe: content enters with IsLive=false, so VisualTree is never cached.
			// In WinUI, the global Unsafe_IslandsIncompatible_CoreWindowContentRoot fallback
			// handles this, but that approach fails in multi-window scenarios. Recovering
			// from the parent is more robust and avoids the single-ContentRoot assumption.
			visualTree ??= (this.GetParent() as DependencyObject)?.GetVisualTree();
#endif
			//This is a dead enter to register any keyboard accelerators that may be present in the Flyout Content
			var newParams = new EnterParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = visualTree, Depth = 0 };
			content.Enter(pNamescopeOwner, newParams);
		}
	}

	internal override void PropagateKeyboardAcceleratorLeave(DependencyObject pNamescopeOwner, LeaveParams @params)
	{
		base.PropagateKeyboardAcceleratorLeave(pNamescopeOwner, @params);

		var content = Content;
		if (content is not null)
		{
			var visualTree = @params.VisualTree;
#if HAS_UNO // Uno specific: recover VisualTree from parent (see Enter above for rationale).
			visualTree ??= (this.GetParent() as DependencyObject)?.GetVisualTree();
#endif
			var newParams = new LeaveParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = visualTree };
			content.Leave(pNamescopeOwner, newParams);
		}
	}
}
