#if __ANDROID__

namespace Windows.Networking.Connectivity
{
	public partial class IPInformation
	{
		public byte? PrefixLength { get; internal set; }
	}
}
#endif
