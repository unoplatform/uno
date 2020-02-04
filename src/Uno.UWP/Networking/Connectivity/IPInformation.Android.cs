#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Networking.Connectivity
{
	public partial class IPInformation
	{
		public byte? PrefixLength { get; internal set; }
	}
}

#endif
