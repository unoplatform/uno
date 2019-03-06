#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DesignMode 
	{
		// Skipping already declared property DesignModeEnabled
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool DesignMode2Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DesignMode.DesignMode2Enabled is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DesignMode.DesignMode2Enabled.get
		// Forced skipping of method Windows.ApplicationModel.DesignMode.DesignModeEnabled.get
	}
}
