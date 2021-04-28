#nullable enable

using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Markup
{
	/// <summary>
	/// Reference holder for XAML generated x:Name and other markup elements.
	/// </summary>
	/// <remarks>
	/// Storing an instance in a holder is weak as it assumes that the
	/// visual tree holds strong references to the target.
	/// </remarks>
	public class ComponentHolder
	{
		private ManagedWeakReference? _instanceRef;

		/// <summary>
		/// Creates an instance of ComponentHolder
		/// </summary>
		public ComponentHolder() { }

		/// <summary>
		/// The component instance
		/// </summary>
		public object? Instance
		{
			get => _instanceRef?.Target;
			set => _instanceRef = WeakReferencePool.RentWeakReference(this, value);
		}
	}
}
