using System;
using Windows.UI.ViewManagement;

namespace Uno.Devices.Sensors
{
	public interface INativeDualScreenProvider
	{
		bool IsDualScreen { get; }

		bool? IsSpanned { get; }
	}
}
