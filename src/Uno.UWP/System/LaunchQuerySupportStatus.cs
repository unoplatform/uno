#if __MOBILE__
namespace Windows.System
{
	public enum LaunchQuerySupportStatus
	{
		Available,
		AppNotInstalled,
		AppUnavailable,
		NotSupported,
		Unknown,
	}
}
#endif
