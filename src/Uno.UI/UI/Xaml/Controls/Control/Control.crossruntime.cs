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

#if !__NETSTD_REFERENCE__
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		if (@params.IsLive)
		{
			// if (SupportsBuiltInStyles() && !m_fIsBuiltInStyleApplied)
			// {
			// 	// When we apply the built-in style, we may resolve theme resources in doing so
			// 	// that haven't yet been resolved - for example, if a property value references
			// 	// a resource that then references another resource.
			// 	// We need to make sure we're operating under the correct theme during that resolution.
			// 	bool removeRequestedTheme = false;
			// 	const auto theme = GetTheme();
			// 	const auto oldRequestedThemeForSubTree = GetRequestedThemeForSubTreeFromCore();
			//
			// 	if (theme != Theming::Theme::None && Theming::GetBaseValue(theme) != oldRequestedThemeForSubTree)
			// 	{
			// 		SetRequestedThemeForSubTreeOnCore(theme);
			// 		removeRequestedTheme = true;
			// 	}
			//
			// 	auto themeGuard = wil::scope_exit([&] {
			// 		if (removeRequestedTheme)
			// 		{
			// 			SetRequestedThemeForSubTreeOnCore(oldRequestedThemeForSubTree);
			// 		}
			// 	});
			//
			// 	IFC_RETURN(ApplyBuiltInStyle());
			// }

			// Initialize StateTriggers at this time.  We need to wait for this to enter a visual tree
			// since we need for it to be part of the main visual tree to know which visual tree's
			// qualifier context to use.  We also need to force an update because our visual tree root
			// may have changed since the last enter.
			VisualStateManager.InitializeStateTriggers(this, true /* forceUpdate */);
		}
	}
#endif
}
