#nullable enable
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.Linux.FrameBuffer;

// No OS keyboard service is available on the Linux framebuffer host. Honest no-op.
internal sealed class FrameBufferInputPaneExtension : IInputPaneExtension
{
	internal static FrameBufferInputPaneExtension Instance { get; } = new();

	private FrameBufferInputPaneExtension()
	{
	}

	public bool TryShow() => false;

	public bool TryHide() => false;
}
