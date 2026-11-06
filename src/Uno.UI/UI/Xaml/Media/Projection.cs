#if __SKIA__
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Provides a base class for projections, which describe how to transform an object in 3-D space using perspective transforms.
/// </summary>
public partial class Projection : DependencyObject, IMultiParentShareableDependencyObject
{
	private WeakReference<UIElement> _owner;

	/// <summary>
	/// Initializes a new instance of the Projection class.
	/// </summary>
	protected Projection()
	{
	}

	private WeakEventHelper.WeakEventCollection _changedHandlers;

	/// <summary>
	/// Registers a handler notified when any property affecting the projection changes, without keeping the
	/// handler's target alive: a projection shared through a Style setter outlives the elements it is applied to.
	/// </summary>
	/// <returns>A disposable that keeps the registration alive; dispose it to unregister.</returns>
	internal IDisposable RegisterChanged(EventHandler handler)
		=> WeakEventHelper.RegisterEvent(
			_changedHandlers ??= new(),
			handler,
			(h, s, e) => (h as EventHandler)?.Invoke(s, (EventArgs)e));

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
	/// Notifies the handlers registered through <see cref="RegisterChanged"/>.
	/// </summary>
	private protected void OnPropertyChanged()
	{
		_changedHandlers?.Invoke(this, EventArgs.Empty);
	}
}
#endif
