using System;
using Windows.UI.ViewManagement;

namespace Uno.Devices.Sensors
{
	public interface INativeDualScreenProvider
	{
		bool? IsSpanned { get; }

		bool SupportsSpanning { get; }
	}
}
