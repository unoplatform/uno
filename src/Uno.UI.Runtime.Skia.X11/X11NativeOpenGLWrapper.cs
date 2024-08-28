using System;
using Microsoft.UI.Xaml;
using Uno.Graphics;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeOpenGLWrapper : INativeOpenGLWrapper
{
	public void CreateContext(UIElement element) => throw new NotImplementedException();

	public object CreateGLSilkNETHandle() => throw new NotImplementedException();

	public void DestroyContext() => throw new NotImplementedException();

	public IDisposable MakeCurrent() => throw new NotImplementedException();
}
