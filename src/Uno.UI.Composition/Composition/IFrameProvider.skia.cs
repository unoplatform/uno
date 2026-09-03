#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

internal interface IFrameProvider : IDisposable
{
	IImage? CurrentImage { get; }
}
