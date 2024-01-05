using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioToolbox;
using UIKit;

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
		private const int PopSoundId = 1520;
		private static VibrationDevice? _instance;

		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault() =>
			_instance ?? (_instance = new VibrationDevice());

		/// <summary>
		/// iOS vibration support is quite limited,
		/// we can produce only very short vibration and one second vibration
		/// </summary>
		/// <param name="duration"></param>
		public void Vibrate(TimeSpan duration)
		{
			if (duration.TotalMilliseconds < 200)
			{
				var pop = new SystemSound(PopSoundId);
				pop.PlaySystemSound();
			}
			else
			{
				SystemSound.Vibrate.PlaySystemSound();
			}
		}
	}
}
