using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public partial class TimeTrigger : IBackgroundTrigger
	{
		public uint FreshnessTime { get; }
		public bool OneShot { get; }

		public TimeTrigger(uint freshnessTime, bool oneShot)
		{
			if (freshnessTime < 15)
			{
				throw new ArgumentOutOfRangeException("TimeTrigger.FreshnessTime should be > 15 minutes");
			}
			FreshnessTime = freshnessTime;
			OneShot = oneShot;
		}
	}
}
