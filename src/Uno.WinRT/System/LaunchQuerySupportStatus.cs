#if __ANDROID__ || __IOS__
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
