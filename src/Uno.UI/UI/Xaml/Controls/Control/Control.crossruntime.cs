using System.Linq;
using Uno.Extensions;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	/// <summary>
	/// Gets the first sub-view of this control or null if there is none
	/// </summary>
	internal IFrameworkElement GetTemplateRoot()
	{
		return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
	}

	internal virtual bool IsDelegatingFocusToTemplateChild() => false;

#if !__NETSTD_REFERENCE__
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		if (@params.IsLive)
		{
			// MUX reference (CControl::EnterImpl): WinUI applies the built-in (lightweight) style here, and
			// because that can resolve nested {ThemeResource}s, it temporarily pushes the element's own theme
			// onto the core's subtree-theme slot around ApplyBuiltInStyle (SetRequestedThemeForSubTreeOnCore
			// guarded by a scope_exit). Uno does NOT port that theme push: built-in/default styles are applied
			// via FrameworkElement.OnLoadingPartial → ApplyStyles()/ApplyDefaultStyle(), and the push is
			// obsolete after Phase 4 — built-in-style {ThemeResource}s resolve against the owner's effective
			// theme (ThemeResolution.ResolveOwnerTheme), threaded through the resolution chain (D3, Mechanism 1).

			// Initialize StateTriggers at this time.  We need to wait for this to enter a visual tree
			// since we need for it to be part of the main visual tree to know which visual tree's
			// qualifier context to use.  We also need to force an update because our visual tree root
			// may have changed since the last enter.
			VisualStateManager.InitializeStateTriggers(this, true /* forceUpdate */);
		}
	}
#endif
}
