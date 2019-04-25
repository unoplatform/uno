using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Extensions;

namespace Windows.Storage
{
	partial class StorageFolder
	{
		internal void MakePersistent()
			=> MakePersistent(this);

		internal static void MakePersistent(params StorageFolder[] folders)
		{
			var parms = new StorageFolderMakePersistentParams()
			{
				Paths = folders.SelectToArray(f => f.Path),
				Paths_Length = folders.Length
			};

			TSInteropMarshaller.InvokeJS<StorageFolderMakePersistentParams>("UnoStatic_Windows_Storage_StorageFolder:makePersistent", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct StorageFolderMakePersistentParams
		{
			public int Paths_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
			public string[] Paths;
		}
	}
}
