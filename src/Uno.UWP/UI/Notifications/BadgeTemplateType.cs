#nullable disable

#if !__ANDROID__

namespace Windows.UI.Notifications
{
	/// <summary>
	/// Specifies the template to use for a tile's badge overlay. Used by BadgeUpdateManager.GetTemplateContent.
	/// </summary>
	public enum BadgeTemplateType
	{
		/// <summary>
		/// A system-provided glyph image.
		/// </summary>
		BadgeGlyph,

		/// <summary>
		/// A numerical badge.
		/// </summary>
		BadgeNumber,
	}
}
#endif
