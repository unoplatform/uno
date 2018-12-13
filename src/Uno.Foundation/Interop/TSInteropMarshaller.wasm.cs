using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Foundation;

namespace Uno.Foundation.Interop
{
	public static class TSInteropMarshaller
	{
		public const UnmanagedType LPUTF8Str = (UnmanagedType)48;

		/// <summary>
		/// Prints the actual offsets of the structures present in <see cref="WindowManagerInterop"/> for debugging purposes.
		/// </summary>
		internal static void GenerateTSMarshallingLayouts()
		{
			// Uncomment this to troubshoot this field offsets.
			//
			// Console.WriteLine("Generating layouts");
			// foreach (var p in typeof(WindowManagerInterop).GetNestedTypes(System.Reflection.BindingFlags.NonPublic).Where(t => t.IsValueType))
			// {
			// 		var sb = new StringBuilder();
			   
			// 		Console.WriteLine($"class {p.Name}:");
			   
			// 		foreach (var field in p.GetFields())
			// 		{
			// 			var fieldOffset = Marshal.OffsetOf(p, field.Name);
			// 			Console.WriteLine($"\t{field.Name} : {fieldOffset}");
			// 		}
			// }
		}

		public static void InvokeJS<TParam>(string methodName, TParam paramStruct)
		{
			var pParms = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TParam)));

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms);
			}
			finally
			{
				Marshal.DestroyStructure(pParms, typeof(TParam));
				Marshal.FreeHGlobal(pParms);
			}
		}

		public static TRet InvokeJS<TParam, TRet>(string methodName, TParam paramStruct) where TRet : new()
		{
			var pParms = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TParam)));
			var pReturnValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TRet)));

			var returnValue = new TRet();

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);
				Marshal.StructureToPtr(returnValue, pReturnValue, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, pReturnValue);

				returnValue = (TRet)Marshal.PtrToStructure(pReturnValue, typeof(TRet));
				return returnValue;
			}
			finally
			{
				Marshal.DestroyStructure(pParms, typeof(TParam));
				Marshal.FreeHGlobal(pParms);

				Marshal.DestroyStructure(pReturnValue, typeof(TRet));
				Marshal.FreeHGlobal(pReturnValue);
			}
		}

	}
}
