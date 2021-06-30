#nullable enable

using Windows.Devices.Haptics;
using System.Linq;
using Tizen.Applications;
using Uno.Extensions.Specialized;
using Uno.UI.Runtime.Skia.Tizen.Helpers;

namespace Uno.UI.Runtime.Skia.Tizen.Devices.Haptics
{
	public class TizenVibrationDeviceExtension : IVibrationDeviceExtension
	{
		private const string TizenPrivilege = "http://tizen.org/privilege/haptic";

		public TizenVibrationDeviceExtension(object owner)
		{
		}

		public VibrationAccessStatus AccessStatus
		{
			get
			{
				if (PrivilegeHelper.IsDeclared(TizenPrivilege))
				{
					return VibrationAccessStatus.Allowed;
				}
				return VibrationAccessStatus.DeniedBySystem;
			}
		}

		public bool IsAvailable => true;
	}
}
