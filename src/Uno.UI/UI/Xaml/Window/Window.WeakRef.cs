using Uno.UI.DataBinding;

namespace Windows.UI.Xaml;

partial class Window : IWeakReferenceProvider
{
	private ManagedWeakReference _selfWeakReference;

	/// <summary>
	/// Implements IWeakReferenceProvider which is required by support XamlGenerator
	/// to generate Window-based XAML files.
	/// </summary>
	ManagedWeakReference IWeakReferenceProvider.WeakReference => _selfWeakReference ??= WeakReferencePool.RentSelfWeakReference(this);
}
