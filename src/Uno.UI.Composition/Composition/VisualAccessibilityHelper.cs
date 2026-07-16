#nullable enable

using System;
using Microsoft.UI.Composition;

namespace Uno.Helpers;

internal static class VisualAccessibilityHelper
{
	internal static Action<Visual>? ExternalOnVisualOffsetOrSizeChanged { get; set; }
}
