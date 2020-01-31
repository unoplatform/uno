#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallHistoryStore
	{
		public PhoneCallHistoryEntryReader GetEntryReader() => new PhoneCallHistoryEntryReader();

	}
}

#endif 
