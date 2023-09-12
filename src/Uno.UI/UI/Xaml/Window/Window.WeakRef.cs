using Uno.UI.DataBinding;

namespace Windows.UI.Xaml;

partial class Window : IWeakReferenceProvider
{
	private ManagedWeakReference _selfWeakReference;

	ManagedWeakReference IWeakReferenceProvider.WeakReference => _selfWeakReference ??= WeakReferencePool.RentSelfWeakReference(this);
}
