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
// NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Reflection;

namespace Uno.Xaml.Schema
{
	public class XamlMemberInvoker
	{
		public static XamlMemberInvoker UnknownInvoker
		{
			get;
		} = new XamlMemberInvoker ();

		protected XamlMemberInvoker ()
		{
		}

		public XamlMemberInvoker (XamlMember member)
		{
			if (member == null)
			{
				throw new ArgumentNullException (nameof(member));
			}

			_member = member;
		}

		private readonly XamlMember _member;

		public MethodInfo UnderlyingGetter {
			get { return _member != null ? _member.UnderlyingGetter : null; }
		}

		public MethodInfo UnderlyingSetter {
			get { return _member != null ? _member.UnderlyingSetter : null; }
		}

		private void ThrowIfUnknown ()
		{
			if (_member == null)
			{
				throw new NotSupportedException ("Current operation is invalid for unknown member.");
			}
		}

		public virtual object GetValue (object instance)
		{
			ThrowIfUnknown ();
			if (instance == null)
			{
				throw new ArgumentNullException (nameof(instance));
			}

			if (_member is XamlDirective)
			{
				throw new NotSupportedException (string.Format ("not supported operation on directive member {0}", _member));
			}

			if (UnderlyingGetter == null)
			{
				throw new NotSupportedException (string.Format ("Attempt to get value from write-only property or event {0}", _member));
			}

			return UnderlyingGetter.Invoke (instance, new object [0]);
		}
		public virtual void SetValue (object instance, object value)
		{
			ThrowIfUnknown ();
			if (instance == null)
			{
				throw new ArgumentNullException (nameof(instance));
			}

			if (_member is XamlDirective)
			{
				throw new NotSupportedException (string.Format ("not supported operation on directive member {0}", _member));
			}

			if (UnderlyingSetter == null)
			{
				throw new NotSupportedException (string.Format ("Attempt to set value from read-only property {0}", _member));
			}

			if (_member.IsAttachable)
			{
				UnderlyingSetter.Invoke (null, new[] {instance, value});
			}
			else
			{
				UnderlyingSetter.Invoke (instance, new[] {value});
			}
		}

		public virtual ShouldSerializeResult ShouldSerializeValue (object instance)
		{
			throw new NotImplementedException ();
		}
	}
}
