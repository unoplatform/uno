using Android.Views;
using Uno.UI.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class MenuFlyout
{
	internal override View NativeTarget
	{
		get
		{
			if (GetActualTarget() is { } actualTarget && actualTarget != Target)
			{
				return actualTarget;
			}

			return null;
		}
	}

	private View GetActualTarget()
	{
		if (Target is AppBarButton appBarButton
		&& ((AView)Target).Parent == null // View.Parent (IViewParent) is the visual parent (hidden using `new` modifier).
		&& Target.Parent is CommandBar commandBar // FrameworkElement.Parent (DependencyObject) is the logical parent.
		&& commandBar.FindViewById(appBarButton.GetHashCode()) is View actionMenuItemView)
		{
			// This AppBarButton doesn't exist in the visual tree and is likely rendered natively as an ActionMenuItemView.
			return actionMenuItemView;
		}

		return Target;
	}
}
