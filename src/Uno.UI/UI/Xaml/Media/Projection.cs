#if __SKIA__
using System;
using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Provides a base class for projections, which describe how to transform an object in 3-D space using perspective transforms.
/// </summary>
public partial class Projection : DependencyObject
{
	private WeakReference<UIElement> _owner;

	/// <summary>
	/// Initializes a new instance of the Projection class.
	/// </summary>
	protected Projection()
	{
	}

	/// <summary>
	/// Event raised when any property affecting the projection changes.
	/// </summary>
	internal event EventHandler Changed;

	/// <summary>
	/// Gets or sets the UIElement that owns this projection.
	/// </summary>
	internal UIElement Owner
	{
		get => _owner?.TryGetTarget(out var target) == true ? target : null;
		set => _owner = value is not null ? new WeakReference<UIElement>(value) : null;
	}

	/// <summary>
	/// Calculates the projection matrix for the specified element size.
	/// </summary>
	/// <param name="elementSize">The size of the element being projected.</param>
	/// <returns>The 4x4 projection matrix.</returns>
	internal virtual Matrix4x4 GetProjectionMatrix(Size elementSize) => Matrix4x4.Identity;

	/// <summary>
	/// Raises the Changed event.
	/// </summary>
	private protected void OnPropertyChanged()
	{
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
#endif
