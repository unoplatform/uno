using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Windows.Foundation;
using Uno.Foundation.Interop;
using System.Net.WebSockets;
using System.Diagnostics;

using System.Runtime.InteropServices.JavaScript;

namespace SamplesApp.UnitTests.TSBindings
{
	[TestClass]
	[Preserve(AllMembers = true)]

	public class TSBindingsTests
	{
		[TestMethod]
		public void When_TestPerf()
		{
			var sw1 = Stopwatch.StartNew();
			for (int i = 0; i < 1000; i++)
			{
				string r = TestImport.When_SingleStringNet7(i.ToString());
			}

			Console.WriteLine($"net7 interop: {sw1.Elapsed}");

			var sw2 = Stopwatch.StartNew();
			for (int i = 0; i < 1000; i++)
			{
				var param = new When_SingleStringParams()
				{
					MyString = "This is 42"
				};

				var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_SingleString", param, typeof(GenericReturn));

			}

			Console.WriteLine($"uno ts interop: {sw2.Elapsed}");
		}

		[TestMethod]
		public void When_IntPtr()
		{
			var param = new When_IntPtrParams()
			{
				Id = (IntPtr)42
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_IntPtr", param, typeof(GenericReturn));

			Assert.AreEqual("42", ret.Value);
		}

		[TestMethod]
		public void When_IntPtr_Zero()
		{
			var param = new When_IntPtrParams()
			{
				Id = (IntPtr)0
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_IntPtr_Zero", param, typeof(GenericReturn));

			Assert.AreEqual("0", ret.Value);
		}

		[TestMethod]
		public void When_SingleString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = "This is 42"
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_SingleString", param, typeof(GenericReturn));

			Assert.AreEqual(param.MyString, ret.Value);
		}

		[TestMethod]
		public void When_SingleUnicodeString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = "This is 🤣 🎉"
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_SingleUnicodeString", param, typeof(GenericReturn));

			Assert.AreEqual(param.MyString, ret.Value);
		}

		[TestMethod]
		public void When_NullString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = null
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullString", param, typeof(GenericReturn));

			Assert.AreEqual("true", ret.Value);
		}

		[TestMethod]
		public void When_ArrayOfInt()
		{
			var param = new When_ArrayOfIntParams()
			{
				MyArray_Length = 4,
				MyArray = new[] { 1, 2, 3, 42 }
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfInt", param, typeof(GenericReturn));

			Assert.AreEqual("1;2;3;42", ret.Value);
		}

		[TestMethod]
		public void When_NullArrayOfInt()
		{
			var param = new When_ArrayOfIntParams()
			{
				MyArray_Length = 0,
				MyArray = null
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullArrayOfInt", param, typeof(GenericReturn));

			Assert.AreEqual("true", ret.Value);
		}
	}

	partial class TestImport
	{
		[JSImport("globalThis.When_SingleStringNet7")]
		internal static partial string When_SingleStringNet7(string value);
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct When_ArrayOfIntParams
	{
		public int MyArray_Length;

		[MarshalAs(UnmanagedType.LPArray)]
		public int[] MyArray;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct When_SingleStringParams
	{
		public string MyString;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct When_IntPtrParams
	{
		public IntPtr Id;
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct GenericReturn
	{
		public string Value;
	}
}
