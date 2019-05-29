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
		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault()
		{
			return new VibrationDevice();	
		}

		/// <summary>
		/// iOS vibration support is quite limited, so duration is not taken into account
		/// </summary>
		/// <param name="duration"></param>
		public void Vibrate(TimeSpan duration)
		{
			var pop = new SystemSound(1520);
			pop.PlaySystemSound();
		}		
	}
}
#endif
