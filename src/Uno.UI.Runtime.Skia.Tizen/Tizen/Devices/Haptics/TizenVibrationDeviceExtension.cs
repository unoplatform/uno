#nullable enable

using Windows.Devices.Haptics;
using System.Linq;
using Tizen.Applications;
using Uno.Extensions.Specialized;

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
				var packageId = Application.Current.ApplicationInfo.PackageId;
				var package = PackageManager.GetPackage(packageId);

				if (package.Privileges.Any(p=> p == TizenPrivilege))
				{
					return VibrationAccessStatus.Allowed;
				}
				return VibrationAccessStatus.DeniedBySystem;
			}
		}

		public bool IsAvailable => true;
	}
}
