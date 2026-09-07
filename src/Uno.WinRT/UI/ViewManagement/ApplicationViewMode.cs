using Uno;

namespace Windows.UI.ViewManagement
{
	public enum ApplicationViewMode
	{
		Default,

		[NotImplemented]
		CompactOverlay,

#if !__ANDROID__
		[NotImplemented]
#endif
		Spanning,
	}
}
