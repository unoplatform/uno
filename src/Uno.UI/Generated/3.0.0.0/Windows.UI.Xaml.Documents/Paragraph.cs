#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Documents
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Paragraph : global::Windows.UI.Xaml.Documents.Block
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double TextIndent
		{
			get
			{
				return (double)this.GetValue(TextIndentProperty);
			}
			set
			{
				this.SetValue(TextIndentProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Documents.InlineCollection Inlines
		{
			get
			{
				throw new global::System.NotImplementedException("The member InlineCollection Paragraph.Inlines is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextIndentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextIndent", typeof(double), 
			typeof(global::Windows.UI.Xaml.Documents.Paragraph), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public Paragraph() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Documents.Paragraph", "Paragraph.Paragraph()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Documents.Paragraph.Paragraph()
		// Forced skipping of method Windows.UI.Xaml.Documents.Paragraph.Inlines.get
		// Forced skipping of method Windows.UI.Xaml.Documents.Paragraph.TextIndent.get
		// Forced skipping of method Windows.UI.Xaml.Documents.Paragraph.TextIndent.set
		// Forced skipping of method Windows.UI.Xaml.Documents.Paragraph.TextIndentProperty.get
	}
}
