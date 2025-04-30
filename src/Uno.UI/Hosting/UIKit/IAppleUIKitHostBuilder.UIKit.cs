using System.Diagnostics.CodeAnalysis;
namespace Uno.UI.Hosting;

public interface IAppleUIKitHostBuilder
{
	public IAppleUIKitHostBuilder UseUIApplicationDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
#if UIKIT_SKIA
		where T : UnoUIApplicationDelegate;
#else
		where T : Microsoft.UI.Xaml.Application;
#endif
}
