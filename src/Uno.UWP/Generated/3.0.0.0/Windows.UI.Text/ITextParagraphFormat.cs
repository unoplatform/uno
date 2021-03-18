#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ITextParagraphFormat 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.ParagraphAlignment Alignment
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float FirstLineIndent
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect KeepTogether
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect KeepWithNext
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float LeftIndent
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float LineSpacing
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.LineSpacingRule LineSpacingRule
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.MarkerAlignment ListAlignment
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int ListLevelIndex
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int ListStart
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.MarkerStyle ListStyle
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float ListTab
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.MarkerType ListType
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect NoLineNumber
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect PageBreakBefore
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float RightIndent
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect RightToLeft
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float SpaceAfter
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		float SpaceBefore
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.ParagraphStyle Style
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int TabCount
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.FormatEffect WidowControl
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.Alignment.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.Alignment.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.FirstLineIndent.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.KeepTogether.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.KeepTogether.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.KeepWithNext.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.KeepWithNext.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.LeftIndent.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.LineSpacing.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.LineSpacingRule.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListAlignment.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListAlignment.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListLevelIndex.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListLevelIndex.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListStart.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListStart.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListStyle.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListStyle.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListTab.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListTab.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListType.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.ListType.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.NoLineNumber.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.NoLineNumber.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.PageBreakBefore.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.PageBreakBefore.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.RightIndent.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.RightIndent.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.RightToLeft.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.RightToLeft.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.Style.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.Style.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.SpaceAfter.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.SpaceAfter.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.SpaceBefore.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.SpaceBefore.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.WidowControl.get
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.WidowControl.set
		// Forced skipping of method Windows.UI.Text.ITextParagraphFormat.TabCount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void AddTab( float position,  global::Windows.UI.Text.TabAlignment align,  global::Windows.UI.Text.TabLeader leader);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void ClearAllTabs();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void DeleteTab( float position);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.ITextParagraphFormat GetClone();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void GetTab( int index, out float position, out global::Windows.UI.Text.TabAlignment align, out global::Windows.UI.Text.TabLeader leader);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsEqual( global::Windows.UI.Text.ITextParagraphFormat format);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetClone( global::Windows.UI.Text.ITextParagraphFormat format);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetIndents( float start,  float left,  float right);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetLineSpacing( global::Windows.UI.Text.LineSpacingRule rule,  float spacing);
		#endif
	}
}
