using Uno.UI.DataBinding;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// DependencyObject wrapped for POCO to enable x:Bind support
	/// </summary>
	public partial class AttachedDependencyObject : DependencyObject
	{
		internal object Owner { get; }
		internal ManagedWeakReference OwnerWeakReference { get; }

		public AttachedDependencyObject(object owner)
		{
			InitializeBinder();

			Owner = owner;
			OwnerWeakReference = WeakReferencePool.RentWeakReference(this, owner);
		}
	}
}
