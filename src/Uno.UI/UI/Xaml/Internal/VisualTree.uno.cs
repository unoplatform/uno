#nullable enable

using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

internal partial class VisualTree : IWeakReferenceProvider
{
	private const int UnoTopZIndex = int.MaxValue - 100;
	private const int FocusVisualZIndex = UnoTopZIndex + 1;

	private ManagedWeakReference? _selfWeakReference;

	public Canvas? FocusVisualRoot { get; private set; }

	public void EnsureFocusVisualRoot()
	{
		if (FocusVisualRoot == null)
		{
			FocusVisualRoot = new Canvas()
			{
				Background = null,
				IsHitTestVisible = false
			};
			Canvas.SetZIndex(FocusVisualRoot, FocusVisualZIndex);
		}
	}

	ManagedWeakReference IWeakReferenceProvider.WeakReference =>
		_selfWeakReference ??= WeakReferencePool.RentSelfWeakReference(this);
}
