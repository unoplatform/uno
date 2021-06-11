#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
	public   enum BadgeTemplateType 
	{
		#if __ANDROID__ || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		BadgeGlyph = 0,
		#endif
		#if __ANDROID__ || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		BadgeNumber = 1,
		#endif
	}
	#endif
}
