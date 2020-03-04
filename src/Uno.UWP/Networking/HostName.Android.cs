#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Windows.Networking
{
	public partial class HostName : Windows.Foundation.IStringable
	{
		public  string CanonicalName { get; internal set; }
		public  string DisplayName { get; internal set; }
		public  global::Windows.Networking.Connectivity.IPInformation IPInformation { get; internal set; }
		public  string RawName { get; internal set; }
		public  HostNameType Type { get; internal set; }


		internal HostName()
		{
			// without this empty constructor, VStudio shows error
			// in line
			// HostName newHost = new HostName();
			// in file NetworkInformation.cs
		}
	}
}

#endif
