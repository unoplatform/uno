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
		private object? _instance;

		/// <summary>
		/// Creates an instance of ComponentHolder
		/// </summary>
		public ComponentHolder() : this(true) { }

		/// <summary>
		/// Creates an instance of ComponentHolder
		/// </summary>
		public ComponentHolder(bool isWeak) { IsWeak = isWeak; }

		/// <summary>
		/// The component instance
		/// </summary>
		public object? Instance
		{
			get => IsWeak ? _instanceRef?.Target : _instance;

			set
			{
				if (IsWeak)
				{
					_instanceRef = WeakReferencePool.RentWeakReference(this, value);
				}
				else
				{
					_instance = value;
				}
			}
		}

		/// <summary>
		/// Indicates if the reference stored in <see cref="Instance"/> is a weak reference.
		/// </summary>
		public bool IsWeak { get; }
	}
}
