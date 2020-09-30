#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || false || false || false || false || false || false
	public   enum BadgeTemplateType 
	{
		#if __ANDROID__ || false || false || false || false || false || false
		BadgeGlyph = 0,
		#endif
		#if __ANDROID__ || false || false || false || false || false || false
		BadgeNumber = 1,
		#endif
	}
	#endif
}
