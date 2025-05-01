using System.Diagnostics.CodeAnalysis;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.UI.Hosting;

public interface IAppleUIKitSkiaHostBuilder
{
	public IAppleUIKitSkiaHostBuilder UseUIApplicationDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
		where T : UnoUIApplicationDelegate;
}
