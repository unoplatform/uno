#if __WASM__
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Windows.Foundation;
using Uno.Foundation.Interop;

namespace SamplesApp.UnitTests.TSBindings
{
	[TestClass]
	[Preserve(AllMembers = true)]

	public class TSBindingsTests
	{
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

		[TestMethod]
		public void When_ArrayOfStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 4,
				MyArray = new[] { "1", "2", "3", "42" }
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfStrings", param, typeof(GenericReturn));

			Assert.AreEqual("1;2;3;42", ret.Value);
		}

		[TestMethod]
		public void When_ArrayOfUnicodeStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 1,
				MyArray = new[] { "🎉🤣😊👆🎁" }
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfUnicodeStrings", param, typeof(GenericReturn));

			Assert.AreEqual(param.MyArray[0], ret.Value);
		}

		[TestMethod]
		public void When_NullArrayOfStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 0, 
				MyArray = null
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullArrayOfStrings", param, typeof(GenericReturn));

			Assert.AreEqual("true", ret.Value);
		}

		[TestMethod]
		public void When_ArrayOfNullStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 4,
				MyArray = new string[4]
			};

			var ret = (GenericReturn)TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfNullStrings", param, typeof(GenericReturn));

			Assert.AreEqual("true;true;true;true", ret.Value);
		}
	}

	[TSInteropMessage]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct When_ArrayOfStringsParams
	{
		public int MyArray_Length;

		[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
		public string[] MyArray;
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
#endif
