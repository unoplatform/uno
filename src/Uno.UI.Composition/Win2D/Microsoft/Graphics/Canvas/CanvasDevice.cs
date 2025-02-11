using System;

namespace Microsoft.Graphics.Canvas;

internal class CanvasDevice : ICanvasResourceCreator, IDisposable
{
	private static Lazy<CanvasDevice> _sharedDeviceLazy = new(() => new());

	public CanvasDevice() { }

	public CanvasDevice(bool forceSoftwareRenderer) { }

	public CanvasDevice Device => this;

	public static CanvasDevice GetSharedDevice() => _sharedDeviceLazy.Value;

	public static CanvasDevice GetSharedDevice(bool forceSoftwareRenderer) => _sharedDeviceLazy.Value;

	public void Dispose() { }
}
