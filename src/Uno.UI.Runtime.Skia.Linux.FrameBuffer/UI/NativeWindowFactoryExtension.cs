﻿#nullable enable

using System;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly IXamlRootHost _host;

	private Window? _initialWindow;

	internal NativeWindowFactoryExtension(IXamlRootHost host)
	{
		_host = host;
	}

	public bool SupportsClosingCancellation => false;

	public bool SupportsMultipleWindows => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		if (_initialWindow is not null && _initialWindow != window)
		{
			throw new InvalidOperationException("FrameBuffer currently supports single window only");
		}

		_initialWindow = window;
		FrameBufferWindowWrapper.Instance.SetWindow(window, xamlRoot);
		XamlRootMap.Register(xamlRoot, _host);

		return FrameBufferWindowWrapper.Instance;
	}
}
