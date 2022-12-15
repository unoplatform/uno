using System;
using System.Runtime.InteropServices;

namespace Uno.Foundation.Interop
{
	internal static partial class TSInteropMarshaller
	{
		public ref struct StructPtr
		{
			private IntPtr _ptr;

			private readonly Type _targetType;

			public StructPtr(IntPtr ptr, Type targetType)
			{
				_ptr = ptr;

				_targetType = targetType;
			}

			public void Dispose()
			{
				if (_ptr != IntPtr.Zero)
				{
					Marshal.DestroyStructure(_ptr, _targetType);

					Marshal.FreeHGlobal(_ptr);

					_ptr = IntPtr.Zero;
				}
			}

			public static implicit operator IntPtr(StructPtr ptr) => ptr._ptr;
		}
	}
}
