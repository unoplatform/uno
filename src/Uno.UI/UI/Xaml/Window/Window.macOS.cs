#nullable enable

using System;
using Uno.UI.Controls;

namespace Microsoft.UI.Xaml;

public sealed partial class Window
{
	/// <summary>
	/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
	/// This must be set before the <see cref="Window"/> is created (typically when Window.Current is called for the first time),
	/// otherwise it will have no effect.
	/// </summary>
	public static Func<RootViewController>? ViewControllerGenerator { get; set; }
}
