using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Uno.Foundation.Interop
{
	/// <summary>
	/// An handle of a marshalled javascript instance of a managed object
	/// </summary>
	public sealed class JSObjectHandle : IDisposable
	{
		/// <summary>
		/// Creates a new <see cref="JSObjectHandle"/> for the provided object.
		/// </summary>
		/// <param name="target">The object to marshal to javascript</param>
		/// <returns></returns>
		public static JSObjectHandle Create(IJSObject target)
			=> Create(target, JSObjectMetadataProvider.Get(target.GetType()));

		/// <summary>
		/// Creates a new <see cref="JSObjectHandle"/> for the provided object by specifying the <see cref="IJSObjectMetadata"/> of the target.
		/// </summary>
		/// <param name="target">The object to marshal to javascript</param>
		/// <param name="metadata">Metadat of the <paramref name="target"/>.</param>
		/// <returns></returns>
		public static JSObjectHandle Create(IJSObject target, IJSObjectMetadata metadata)
			=> new JSObjectHandle(target, metadata);

		private readonly IJSObjectMetadata _metadata;
		private readonly WeakReference<object> _target;
		private readonly GCHandle _managedGcHandle;
		private readonly IntPtr _managedHandle;
		private readonly long _jsHandle;

		private JSObjectHandle(object target, IJSObjectMetadata metadata)
		{
			_metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			_target = new WeakReference<object>(target ?? throw new ArgumentNullException(nameof(target)));

			_managedGcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
			_managedHandle = GCHandle.ToIntPtr(_managedGcHandle);
			_jsHandle = _metadata.CreateNativeInstance(_managedHandle);
		}

		/// <summary>
		/// Metadata about the marshaled object
		/// </summary>
		public IJSObjectMetadata Metadata => _metadata;

		internal string GetNativeInstance() 
			=> _metadata.GetNativeInstance(_managedHandle, _jsHandle);

		internal bool TryGetManaged(out object target)
			=> _target.TryGetTarget(out target);

		/// <inheritdoc />
		public void Dispose()
		{
			_metadata.DestroyNativeInstance(_managedHandle, _jsHandle);

			GC.SuppressFinalize(this);
		}

		~JSObjectHandle() => Dispose();
	}
}
