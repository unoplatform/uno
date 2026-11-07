using System;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[global::Uno.NotImplemented]
public partial class OrientedVirtualizingPanel
{
	private bool _isUnsupportedLogged;

	public OrientedVirtualizingPanel()
	{
	}

	// Read on every measure of the items presenter: throwing here would take down each layout pass of the window.
	internal override Orientation? PhysicalOrientation
	{
		get
		{
			if (!_isUnsupportedLogged)
			{
				_isUnsupportedLogged = true;

				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(
						$"{GetType().Name} is not supported in Uno Platform yet. " +
						"Use a non-virtualized panel (e.g. ItemsStackPanel) instead.");
				}
			}

			return null;
		}
	}
}
