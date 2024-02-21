#nullable enable

using System;
using Microsoft.UI.Xaml;
using Uno.Disposables;

namespace Uno.UI.Hosting;

internal interface IXamlRootHost
{
	UIElement? RootElement { get; }

	void InvalidateRender();

	// should be cast to a Silk.NET GL object.
	object? GetGL() => null;

	// To prevent concurrent GL operations breaking the state, you should obtain the lock while
	// using GL commands. Make sure to restore all the state to default before unlocking (i.e. unbind
	// all used buffers, textures, etc.)
	IDisposable LockGL() => Disposable.Empty;
}
