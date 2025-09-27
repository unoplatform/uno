using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Uno.UI.Composition;
using Uno.UI.Xaml.Core;
using Windows.UI.Input;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget : ICompositionTarget
{
#pragma warning disable CS0067 // Event is never used
	public static event EventHandler<object> Rendering;
#pragma warning restore CS0067 // Event is never used
}
