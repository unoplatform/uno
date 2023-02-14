#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
	#endif
	public  partial class SymbolIcon : global::Microsoft.UI.Xaml.Controls.IconElement
	{
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public  global::Microsoft.UI.Xaml.Controls.Symbol Symbol
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.Symbol)this.GetValue(SymbolProperty);
			}
			set
			{
				this.SetValue(SymbolProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty SymbolProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Symbol), typeof(global::Microsoft.UI.Xaml.Controls.Symbol), 
			typeof(global::Microsoft.UI.Xaml.Controls.SymbolIcon), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.Symbol)));
		#endif
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public SymbolIcon( global::Microsoft.UI.Xaml.Controls.Symbol symbol) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.SymbolIcon", "SymbolIcon.SymbolIcon(Symbol symbol)");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SymbolIcon.SymbolIcon(Microsoft.UI.Xaml.Controls.Symbol)
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public SymbolIcon() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.SymbolIcon", "SymbolIcon.SymbolIcon()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SymbolIcon.SymbolIcon()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SymbolIcon.Symbol.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SymbolIcon.Symbol.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SymbolIcon.SymbolProperty.get
	}
}
