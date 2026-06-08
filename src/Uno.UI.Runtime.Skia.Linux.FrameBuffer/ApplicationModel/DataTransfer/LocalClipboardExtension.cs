using System;
using Windows.ApplicationModel.DataTransfer;
using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;

/// <summary>
/// A local in-memory clipboard implementation that stores clipboard data
/// within the application process. This is used as a fallback when no
/// system clipboard (e.g. the freedesktop portal) is available.
/// </summary>
internal class LocalClipboardExtension : IClipboardExtension
{
	private DataPackage? _currentContent;

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged()
	{
		// No external monitoring needed for a local clipboard.
	}

	public void StopContentChanged()
	{
		// No external monitoring needed for a local clipboard.
	}

	public void Clear()
	{
		_currentContent = null;
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Flush()
	{
		// Nothing to flush for a local clipboard. Raise ContentChanged for parity
		// with other platform implementations.
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	public DataPackageView GetContent()
	{
		if (_currentContent is not null)
		{
			return _currentContent.GetView();
		}

		return new DataPackage().GetView();
	}

	public void SetContent(DataPackage content)
	{
		ArgumentNullException.ThrowIfNull(content, nameof(content));

		_currentContent = content;
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}
}
