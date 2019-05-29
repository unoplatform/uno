#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioToolbox;

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
		private const int PeekSoundId = 1519;
		private static VibrationDevice _instance = null;

		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault() =>
			_instance ?? (_instance = new VibrationDevice());

		/// <summary>
		/// iOS vibration support is quite limited, so duration is not taken into account
		/// </summary>
		/// <param name="duration"></param>
		public void Vibrate(TimeSpan duration)
		{
			if (duration.TotalMilliseconds < 500)
			{
				var peek = new SystemSound(PeekSoundId);
				peek.PlaySystemSound();
			}
			else
			{
				SystemSound.Vibrate.PlaySystemSound();
			}
		}
	}
}
#endif
