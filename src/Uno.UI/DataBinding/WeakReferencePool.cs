using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A weak reference pool
	/// </summary>
	/// <remarks>
	/// This pool provides <see cref="ManagedWeakReference"/> instances, which are expensive to create (<see cref="GCHandle.Alloc(object)"/> and forces the GC
	/// to track many of those references to the same object. This pool also handles the detection 
	/// of <see cref="IWeakReferenceProvider"/> instances. Those instances are most likely to be 
	/// weakly referenceed, and can provide their own reference more efficiently.
	/// </remarks>
    public static class WeakReferencePool
	{
		private readonly static Stack<ManagedGCHandle> _weakReferencePool = new Stack<ManagedGCHandle>();
		private readonly static object _gate = new object();

		/// <summary>
		/// The maximum number of recycled <see cref="WeakReference"/> that can been pooled
		/// </summary>
		public static int MaxReferences { get; set; } = 500;

		/// <summary>
		/// Determines if the pool can recycle instances returned through <see cref="ReturnWeakReference(object, ManagedWeakReference)"/>
		/// </summary>
		public static bool Enabled { get; set; } = true;

		/// <summary>
		/// Gets the number of currently pooled references
		/// </summary>
		internal static int PooledReferences => _weakReferencePool.Count;

		/// <summary>
		/// Creates a <see cref="ManagedWeakReference"/> for an type implementing <see cref="IWeakReferenceProvider"/>.
		/// </summary>
		/// <param name="target">The instance to create a <see cref="WeakReference"/> for.</param>
		/// <returns>A <see cref="ManagedWeakReference"/>. </returns>
		public static ManagedWeakReference RentSelfWeakReference(IWeakReferenceProvider target)
		{
			var weakTarget = RentFromPool(target);
			return new ManagedWeakReference(weakTarget, weakTarget, true);
		}

		/// <summary>
		/// Gets a <see cref="ManagedWeakReference"/> from the pool.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="target"></param>
		/// <returns>A <see cref="ManagedWeakReference"/>.</returns>
		/// <remarks>
		/// Returned <see cref="ManagedWeakReference"/> are not tracked by the pool
		/// and do not have be returned to pool. Yet, it's best for memory managed to
		/// return them through <see cref="ReturnWeakReference(object, ManagedWeakReference)"/>.
		/// </remarks>
		public static ManagedWeakReference RentWeakReference(object owner, object target)
		{
			if (target is IWeakReferenceProvider provider)
			{
				return provider.WeakReference;
			}
			else
			{
				if (owner is IWeakReferenceProvider ownerProvider)
				{
					return new ManagedWeakReference(ownerProvider.WeakReference.GetUnsafeTargetHandle(), RentFromPool(target), false);
				}
				else
				{
					return new ManagedWeakReference(RentFromPool(owner), RentFromPool(target), false);
				}
			}
		}

		/// <summary>
		/// Return a <see cref="ManagedWeakReference"/> to the pool.
		/// </summary>
		/// <param name="owner">The instance returning the reference to the pool.</param>
		/// <param name="managedWeakReference">A <see cref="ManagedWeakReference"/> to return</param>
		/// <remarks>
		/// The returned <see cref="ManagedWeakReference"/> may not be owner by the <paramref name="owner"/>, 
		/// in which case, the <paramref name="managedWeakReference"/> will not be returned to the pool. If the reference
		/// if an <see cref="IWeakReferenceProvider"/> it is not returned as well.
		/// A <see cref="null"/> <paramref name="managedWeakReference"/> is ignored.
		/// </remarks>
		public static void ReturnWeakReference(object owner, ManagedWeakReference managedWeakReference)
		{
			if (Enabled
				&& managedWeakReference != null
				&& managedWeakReference.Owner == owner
				&& !managedWeakReference.IsSelfReference
			)
			{
				lock (_gate)
				{
					if (_weakReferencePool.Count < MaxReferences)
					{
						ReturnUnsafeWeakReference(managedWeakReference.GetUnsafeTargetHandle());

						if (!(managedWeakReference.Owner is IWeakReferenceProvider))
						{
							// The owner's weak reference may have been borrowed from an IWeakReferenceProvider instance
							// We're not release it if it's the case.
							ReturnUnsafeWeakReference(managedWeakReference.GetUnsafeOwnerHandle());
						}

						managedWeakReference.Dispose();
					}
				}
			}
		}

		private static void ReturnUnsafeWeakReference(ManagedGCHandle handle)
		{
			if (handle != null)
			{
				_weakReferencePool.Push(handle);
			}
		}

		internal static void ClearCache()
		{
			lock (_gate)
			{
				_weakReferencePool.Clear();
			}
		}

		private static ManagedGCHandle RentFromPool(object target)
		{
			if(target == null)
			{
				return null;
			}

			lock (_gate)
			{
				if (_weakReferencePool.Count == 0)
				{
					return new ManagedGCHandle { Target = target };
				}
				else
				{
					var handle = _weakReferencePool.Pop();
					handle.Target = target;

					if(handle.Target != target)
					{
						handle.Target = target;

						throw new InvalidOperationException();
					}

					return handle;
				}
			}
		}
	}
}
