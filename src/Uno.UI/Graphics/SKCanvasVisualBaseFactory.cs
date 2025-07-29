using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace Uno.UI.Graphics;

internal interface SKCanvasVisualBaseFactory
{
	SKCanvasVisualBase CreateInstance(Action<object, Size> renderCallback, Compositor compositor);
}
