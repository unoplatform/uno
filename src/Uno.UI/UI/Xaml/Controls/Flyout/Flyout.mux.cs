using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class Flyout
{
#if UNO_HAS_ENHANCED_LIFECYCLE
	internal override void Enter(DependencyObject pNamescopeOwner, EnterParams @params)
	{
		base.Enter(pNamescopeOwner, @params);

		var content = Content;
		if (content is not null)
		{
			var newParams = new EnterParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = @params.VisualTree };
			content.Enter(newParams, 0);
		}
	}

	internal override void Leave(DependencyObject pNamescopeOwner, LeaveParams @params)
	{
		base.Leave(pNamescopeOwner, @params);

		var content = Content;
		if (content is not null)
		{
			var newParams = new LeaveParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = @params.VisualTree };
			content.Leave(newParams);
		}
	}
#endif
}
