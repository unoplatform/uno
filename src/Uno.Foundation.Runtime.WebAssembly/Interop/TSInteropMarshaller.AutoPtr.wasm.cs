using System;
using System.Runtime.InteropServices;

namespace Uno.Foundation.Interop
{
	internal static partial class TSInteropMarshaller
	{
		public ref struct AutoPtr
		{
			private IntPtr _ptr;

			public AutoPtr(IntPtr ptr)
			{
				_ptr = ptr;
			}

			public void Dispose()
			{
				if (_ptr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(_ptr);

					_ptr = IntPtr.Zero;
				}
			}

			public static implicit operator IntPtr(AutoPtr ptr) => ptr._ptr;
		}
	}
}
