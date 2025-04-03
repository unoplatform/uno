#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation
{
#if __ANDROID__ || __APPLE_UIKIT__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
#endif
	public partial class AutomationProperty
	{
	}
}
