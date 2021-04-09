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

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_IntPtr", param, ret);

			Assert.AreEqual("42", ret.Value.Value);
		}

		[TestMethod]
		public void When_IntPtr_Zero()
		{
			var param = new When_IntPtrParams()
			{
				Id = (IntPtr)0
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_IntPtr_Zero", param, ret);

			Assert.AreEqual("0", ret.Value.Value);
		}

		[TestMethod]
		public void When_SingleString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = "This is 42"
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_SingleString", param, ret);

			Assert.AreEqual(param.MyString, ret.Value.Value);
		}

		[TestMethod]
		public void When_SingleUnicodeString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = "This is 🤣 🎉"
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_SingleUnicodeString", param, ret);

			Assert.AreEqual(param.MyString, ret.Value.Value);
		}

		[TestMethod]
		public void When_NullString()
		{
			var param = new When_SingleStringParams()
			{
				MyString = null
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullString", param, ret);

			Assert.AreEqual("true", ret.Value.Value);
		}

		[TestMethod]
		public void When_ArrayOfInt()
		{
			var param = new When_ArrayOfIntParams()
			{
				MyArray_Length = 4,
				MyArray = new[] { 1, 2, 3, 42 }
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfInt", param, ret);

			Assert.AreEqual("1;2;3;42", ret.Value.Value);
		}

		[TestMethod]
		public void When_NullArrayOfInt()
		{
			var param = new When_ArrayOfIntParams()
			{
				MyArray_Length = 0,
				MyArray = null
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullArrayOfInt", param, ret);

			Assert.AreEqual("true", ret.Value.Value);
		}

		[TestMethod]
		public void When_ArrayOfStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 4,
				MyArray = new[] { "1", "2", "3", "42" }
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfStrings", param, ret);

			Assert.AreEqual("1;2;3;42", ret.Value.Value);
		}

		[TestMethod]
		public void When_ArrayOfUnicodeStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 1,
				MyArray = new[] { "🎉🤣😊👆🎁" }
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfUnicodeStrings", param, ret);

			Assert.AreEqual(param.MyArray[0], ret.Value.Value);
		}

		[TestMethod]
		public void When_NullArrayOfStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 0, 
				MyArray = null
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_NullArrayOfStrings", param, ret);

			Assert.AreEqual("true", ret.Value.Value);
		}

		[TestMethod]
		public void When_ArrayOfNullStrings()
		{
			var param = new When_ArrayOfStringsParams()
			{
				MyArray_Length = 4,
				MyArray = new string[4]
			};

			var ret = new GenericReturn_Wrapper();
			TSInteropMarshaller.InvokeJS("TSBindingsUnitTests:When_ArrayOfNullStrings", param, ret);

			Assert.AreEqual("true;true;true;true", ret.Value.Value);
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

	[StructLayout(LayoutKind.Sequential)]
	public class GenericReturn_Wrapper
	{
		public GenericReturn Value;
	}
}
#endif
