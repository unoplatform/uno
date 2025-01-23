using System;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// An animated Composition.Visual that can be used by other objects, such as an AnimatedVisualPlayer or AnimatedIcon.
/// Extends IAnimatedVisual with methods to create and destroy animations.
/// </summary>
public partial interface IAnimatedVisual2 : IAnimatedVisual, IDisposable
{
	void CreateAnimations();

	void DestroyAnimations();
}
