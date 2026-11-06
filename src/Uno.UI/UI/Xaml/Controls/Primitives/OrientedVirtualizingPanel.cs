using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[global::Uno.NotImplemented]
public partial class OrientedVirtualizingPanel
{
	public OrientedVirtualizingPanel()
	{
	}

	internal override Orientation? PhysicalOrientation => throw new NotSupportedException(
		$"{GetType().Name} is not supported in Uno Platform yet. " +
		"Use a non-virtualized panel (e.g. ItemsStackPanel) instead.");
}
