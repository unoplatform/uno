#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSCoreWindowExtension : ICoreWindowExtension
{
	public static MacOSCoreWindowExtension Instance = new();

	private MacOSCoreWindowExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(ICoreWindowExtension), o => Instance);

	public void ArrangeNativeElement(object owner, object content, Rect arrangeRect)
	{
	}

	public void AttachNativeElement(object owner, object content)
	{
	}

	public void DetachNativeElement(object owner, object content)
	{
	}

	public bool IsNativeElement(object content) => false;

	public bool IsNativeElementAttached(object owner, object nativeElement) => false;

	public Size MeasureNativeElement(object owner, object content, Size size) => new(0,0);
}
