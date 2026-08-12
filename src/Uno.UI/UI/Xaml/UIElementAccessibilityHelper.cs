#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.Helpers;

internal static class UIElementAccessibilityHelper
{
	internal static Action<UIElement, UIElement, int?>? ExternalOnChildAdded { get; set; }
	internal static Action<UIElement, UIElement>? ExternalOnChildRemoved { get; set; }
}
