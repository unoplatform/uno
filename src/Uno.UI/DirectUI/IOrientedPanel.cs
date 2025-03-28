using Windows.UI.Xaml.Controls;

namespace DirectUI;

internal interface IOrientedPanel
{
	Orientation LogicalOrientation { get; }

	Orientation PhysicalOrientation { get; }
}
