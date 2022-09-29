#nullable disable

//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Markup;
using Uno.Xaml;
using Uno.Xaml.Schema;
using System.Xml;
using NUnit.Framework;

namespace MonoTests.Uno.Xaml.Schema
{
	[TestFixture]
	public class XamlMemberInvokerTest
	{
		XamlSchemaContext sctx = new XamlSchemaContext (new XamlSchemaContextSettings ());
		PropertyInfo str_len = typeof (string).GetProperty ("Length");
		PropertyInfo sb_len = typeof (StringBuilder).GetProperty ("Length");
		PropertyInfo xr_resolver = typeof (XmlResolver).GetProperty ("Credentials");
		EventInfo ass_load = typeof (AppDomain).GetEvent ("AssemblyLoad");

		[Test]
		public void ConstructorNull ()
		{
			Assert.Throws(typeof(ArgumentNullException), () => new XamlMemberInvoker (null));
		}

		// Property

		[Test]
		public void FromProperty ()
		{
			var pi = str_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			Assert.AreEqual (pi.GetGetMethod (), i.UnderlyingGetter, "#1");
			Assert.IsNull (i.UnderlyingSetter, "#2");
			Assert.AreEqual (5, i.GetValue ("hello"), "#3");
		}

		[Test]
		public void GetValueNullObject ()
		{
			Assert.Throws(typeof(ArgumentNullException), () =>
			{
				var pi = str_len;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.GetValue(null);
			});
		}

		[Test]
		public void SetValueNullObject ()
		{
			Assert.Throws(typeof(ArgumentNullException), () =>
			{
				var pi = sb_len;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.SetValue(null, 5);
			});
		}

		[Test]
		public void GetValueOnWriteOnlyProperty ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				var pi = xr_resolver;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.GetValue(new XmlUrlResolver());
			});
		}

		[Test]
		public void SetValueOnReadOnlyProperty ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				var pi = str_len;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.SetValue("hello", 5);
			});
		}

		[Test]
		public void SetValueOnReadWriteProperty ()
		{
			var pi = sb_len;
			var i = new XamlMemberInvoker (new XamlMember (pi, sctx));
			var sb = new StringBuilder ();
			i.SetValue (sb, 5);
			Assert.AreEqual (5, sb.Length, "#1");
		}

		[Test]
		public void GetValueOnIrrelevantObject ()
		{
			Assert.Throws(typeof(TargetException), () =>
			{
				var pi = str_len;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.GetValue(new StringBuilder());
			});
		}

		[Test]
		public void GetValueOnTypeValue ()
		{
			var xm = XamlLanguage.Type.GetMember ("Type");
			var i = new XamlMemberInvoker (xm);
			var o = i.GetValue (new TypeExtension (typeof (int)));
			Assert.AreEqual (typeof (int), o, "#1");
		}

		[Test]
		public void GetValueArrayExtension ()
		{
			var xt = sctx.GetXamlType (typeof (TestClass));
			var xm = xt.GetMember ("ArrayMember");
			Assert.IsNotNull (xm, "#-1");
			Assert.AreEqual (XamlLanguage.Array, xm.Type, "#0");
			var o = xm.Invoker.GetValue (new TestClass ());
			Assert.AreEqual (typeof (ArrayExtension), o.GetType (), "#1");
		}

		[Test]
		public void GetValueInitialization ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				var xm = XamlLanguage.Initialization;
				var i = xm.Invoker;
				i.GetValue("foo");
			});
		}

		[Test]
		public void GetValuePositionalParameter ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				var xm = XamlLanguage.PositionalParameters;
				var i = xm.Invoker;
				i.GetValue(new TypeExtension(typeof(int)));
			});
		}

		[Test]
		public void SetValueOnIrrelevantObject ()
		{
			Assert.Throws(typeof(TargetException), () =>
			{
				var pi = sb_len;
				var i = new XamlMemberInvoker(new XamlMember(pi, sctx));
				i.SetValue("hello", 5);
			});
		}

		// Event

		[Test]
		public void FromEvent ()
		{
			var ei = ass_load;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			Assert.IsNull (i.UnderlyingGetter, "#1");
			Assert.AreEqual (ei.GetAddMethod (), i.UnderlyingSetter, "#2");
		}

		[Test]
		public void GetValueOnEvent ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				var ei = ass_load;
				var i = new XamlMemberInvoker(new XamlMember(ei, sctx));
				i.GetValue(AppDomain.CurrentDomain);
			});
		}

		[Test]
		public void SetValueOnEventNull ()
		{
			var ei = ass_load;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			i.SetValue (AppDomain.CurrentDomain, null);
		}

		[Test]
		public void SetValueOnEventValueMismatch ()
		{
			Assert.Throws(typeof(ArgumentException), () =>
			{
				var ei = ass_load;
				var i = new XamlMemberInvoker(new XamlMember(ei, sctx));
				i.SetValue(AppDomain.CurrentDomain, 5);
			});
		}

		void DummyAssemblyLoad (object o, AssemblyLoadEventArgs e)
		{
		}

		[Test]
		public void SetValueOnEvent ()
		{
			var ei = ass_load;
			var i = new XamlMemberInvoker (new XamlMember (ei, sctx));
			i.SetValue (AppDomain.CurrentDomain, new AssemblyLoadEventHandler (DummyAssemblyLoad));
		}

		[Test]
		public void CustomTypeDefaultValues ()
		{
			var i = new MyXamlMemberInvoker ();
			Assert.IsNull (i.UnderlyingGetter, "#1");
			Assert.IsNull (i.UnderlyingSetter, "#2");
		}

		[Test]
		public void UnderlyingGetter ()
		{
			Assert.Throws(typeof(MyException), () =>
			{
				var i = new XamlMemberInvoker(new MyXamlMember(str_len, sctx));
				// call XamlMember's UnderlyingGetter.
				Assert.IsNotNull(i.UnderlyingGetter, "#1");
			});
		}

		[Test]
		public void UnderlyingSetter ()
		{
			Assert.Throws(typeof(MyException), () =>
			{
				var i = new XamlMemberInvoker(new MyXamlMember(str_len, sctx));
				// call XamlMember's UnderlyingSetter.
				Assert.IsNull(i.UnderlyingSetter, "#1");
			});
		}

		class MyXamlMember : XamlMember
		{
			public MyXamlMember (PropertyInfo pi, XamlSchemaContext context)
				: base (pi, context)
			{
			}
			
			protected override MethodInfo LookupUnderlyingGetter ()
			{
				throw new MyException ();
			}

			protected override MethodInfo LookupUnderlyingSetter ()
			{
				throw new MyException ();
			}
		}

		class MyException : Exception
		{
		}

		class MyXamlMemberInvoker : XamlMemberInvoker
		{
		}

		class TestClass
		{
			public TestClass ()
			{
				ArrayMember = new ArrayExtension (typeof (int));
				ArrayMember.AddChild (5);
				ArrayMember.AddChild (3);
				ArrayMember.AddChild (-1);
			}

			public ArrayExtension ArrayMember { get; set; }
		}

		[Test]
		public void UnknownInvokerGetValue ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				XamlMemberInvoker.UnknownInvoker.GetValue(new object());
			});
		}

		[Test]
		public void UnknownInvokerSetValue ()
		{
			Assert.Throws(typeof(NotSupportedException), () =>
			{
				XamlMemberInvoker.UnknownInvoker.SetValue(new object(), new object());
			});
		}

		[Test]
		public void UnknownInvoker ()
		{
			Assert.IsNull (XamlMemberInvoker.UnknownInvoker.UnderlyingGetter, "#1");
			Assert.IsNull (XamlMemberInvoker.UnknownInvoker.UnderlyingSetter, "#2");
		}
	}
}
