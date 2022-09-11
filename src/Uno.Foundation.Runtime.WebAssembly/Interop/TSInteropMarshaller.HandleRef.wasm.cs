#nullable enable

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.Foundation.Runtime.WebAssembly.Helpers;

namespace Uno.Foundation.Interop
{
	internal static partial class TSInteropMarshaller
	{
		/// <summary>
		/// A reference to an instance of <typeparamref name="T"/> shared between javascript and managed code.
		/// </summary>
		/// <typeparam name="T">The type of the shared value</typeparam>
		public sealed class HandleRef<T> : IDisposable
			where T : struct
		{
			private readonly string? _jsDisposeMethodName;

			private int _isDisposed;

			/// <summary>
			/// DO NOT USE, use <see cref="TSInteropMarshaller.Allocate{T}"/> instead.
			/// </summary>
			public HandleRef(string? jsDisposeMethodName)
			{
				_jsDisposeMethodName = jsDisposeMethodName;
				Type = typeof(T);
				Handle = Marshal.AllocHGlobal(Marshal.SizeOf(Type));

				DumpStructureLayout(Type);

				// Make sure to init the allocated memory
				Marshal.StructureToPtr(default(T), Handle, false);
			}

			/// <summary>
			/// The type of the shared instance.
			/// </summary>
			public Type Type { get; }

			/// <summary>
			/// The pointer of the shared instance.
			/// </summary>
			public IntPtr Handle { get; }

			/// <summary>
			/// The value of the shared instance.
			/// Getting or setting this value will read/write the value from/into the shared memory space.
			/// </summary>
			public T Value
			{
				get
				{
					CheckDisposed();
					return (T)(Marshal.PtrToStructure(Handle, Type)!);
				}
				set
				{
					CheckDisposed();
					Marshal.StructureToPtr(value, Handle, true);
				}
			}

			private void CheckDisposed()
			{
				if (_isDisposed != 0)
				{
					throw new ObjectDisposedException(GetType().Name, "Marshalled object have been disposed.");
				}
			}

			/// <inheritdoc />
			public void Dispose()
			{
				if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
				{
					Marshal.DestroyStructure(Handle, Type);
					Marshal.FreeHGlobal(Handle);

					if (_jsDisposeMethodName.HasValue())
					{
						WebAssemblyRuntime.InvokeJSUnmarshalled(_jsDisposeMethodName!, Handle);
					}

					GC.SuppressFinalize(this);
				}
			}

			~HandleRef()
			{
				Dispose();
			}
		}
	}
}
