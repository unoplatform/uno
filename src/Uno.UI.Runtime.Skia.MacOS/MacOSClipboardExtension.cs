#nullable enable

using System;

using Windows.ApplicationModel.DataTransfer;

using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSClipboardExtension : IClipboardExtension
{
	public static MacOSClipboardExtension Instance = new();

	private MacOSClipboardExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IClipboardExtension), o => Instance);

#pragma warning disable CS0067
	public event EventHandler<object>? ContentChanged;
#pragma warning restore CS0067

	public void Clear() => NativeUno.uno_clipboard_clear();

	public void Flush()
	{
		// nothing to do for macOS
	}

	public DataPackageView GetContent() => throw new NotImplementedException();
	public void SetContent(DataPackage content) => throw new NotImplementedException();

	public void StartContentChanged() => NativeUno.uno_clipboard_start_content_changed();

	public void StopContentChanged() => NativeUno.uno_clipboard_stop_content_changed();
}
