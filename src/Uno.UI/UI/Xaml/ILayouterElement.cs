#nullable enable

#if !UNO_REFERENCE_API
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.UI;

namespace Microsoft.UI.Xaml;

internal partial interface ILayouterElement
{
	internal Size Measure(Size availableSize);

	internal void Arrange(Rect finalRect);
}

#endif
