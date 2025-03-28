#nullable enable

using System.ComponentModel;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// The WeakResourceInitializer is responsible for creating resources with a WeakReference to the owner of the resource.
	/// </summary>
	/// <remarks>
	/// This class is used by the XAML source generator to propagate the top-level owner
	/// of the resources in order for code-behind references such as events can be resolved
	/// properly.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class WeakResourceInitializer
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		public delegate object ResourceInitializerWithOwner(object? owner);

		private readonly ManagedWeakReference _owner;

		public WeakResourceInitializer(object owner, ResourceInitializerWithOwner initializer)
		{
			_owner = WeakReferencePool.RentWeakReference(this, owner);
			Initializer = () => initializer(_owner?.Target);
		}

		public ResourceDictionary.ResourceInitializer Initializer { get; }
	}
}
