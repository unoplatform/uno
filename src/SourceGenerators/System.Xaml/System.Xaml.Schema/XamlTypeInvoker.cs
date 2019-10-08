//
// Copyright (C) 2010 Novell Inc. http://novell.com
// Copyright (C) 2012 Xamarin Inc. http://xamarin.com
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Markup;

namespace Uno.Xaml.Schema
{
	public class XamlTypeInvoker
	{
		public static XamlTypeInvoker UnknownInvoker { get; } = new XamlTypeInvoker();

		protected XamlTypeInvoker ()
		{
		}
		
		public XamlTypeInvoker (XamlType type)
		{
			_type = type ?? throw new ArgumentNullException (nameof(type));
		}

		private readonly XamlType _type;

		private void ThrowIfUnknown ()
		{
			if (_type == null || _type.UnderlyingType == null)
			{
				throw new NotSupportedException (string.Format ("Current operation is valid only when the underlying type on a XamlType is known, but it is unknown for '{0}'", _type));
			}
		}

		public EventHandler<XamlSetMarkupExtensionEventArgs> SetMarkupExtensionHandler {
			get { return _type == null ? null : _type.SetMarkupExtensionHandler; }
		}

		public EventHandler<XamlSetTypeConverterEventArgs> SetTypeConverterHandler {
			get { return _type == null ? null : _type.SetTypeConverterHandler; }
		}

		public virtual void AddToCollection (object instance, object item)
		{
			if (instance == null)
			{
				throw new ArgumentNullException (nameof(instance));
			}

			if (item == null)
			{
				throw new ArgumentNullException (nameof(item));
			}

			var ct = instance.GetType ();
			var xct = _type == null ? null : _type.SchemaContext.GetXamlType (ct);
			MethodInfo mi = null;

			// FIXME: this method lookup should be mostly based on GetAddMethod(). At least iface method lookup must be done there.
			if (_type != null && _type.UnderlyingType != null) {
				if (xct != null && !xct.IsCollection) // not sure why this check is done only when UnderlyingType exists...
				{
					throw new NotSupportedException (string.Format ("Non-collection type '{0}' does not support this operation", xct));
				}

				if (ct.IsAssignableFrom (_type.UnderlyingType))
				{
					mi = GetAddMethod (_type.SchemaContext.GetXamlType (item.GetType ()));
				}
			}

			if (mi == null) {
				if (ct.IsGenericType) {
					mi = ct.GetMethod ("Add", ct.GetGenericArguments ());
					if (mi == null)
					{
						mi = LookupAddMethod (ct, typeof (ICollection<>).MakeGenericType (ct.GetGenericArguments ()));
					}
				} else {
					mi = ct.GetMethod ("Add", new[] {typeof (object)});
					if (mi == null)
					{
						mi = LookupAddMethod (ct, typeof (IList));
					}
				}
			}

			if (mi == null)
			{
				throw new InvalidOperationException (string.Format ("The collection type '{0}' does not have 'Add' method", ct));
			}

			mi.Invoke (instance, new[] {item});
		}

		public virtual void AddToDictionary (object instance, object key, object item)
		{
			if (instance == null)
			{
				throw new ArgumentNullException (nameof(instance));
			}

			var t = instance.GetType ();
			// FIXME: this likely needs similar method lookup to AddToCollection().

			MethodInfo mi;
			if (t.IsGenericType) {
				mi = instance.GetType ().GetMethod ("Add", t.GetGenericArguments ());
				if (mi == null)
				{
					mi = LookupAddMethod (t, typeof (IDictionary<,>).MakeGenericType (t.GetGenericArguments ()));
				}
			} else {
				mi = instance.GetType ().GetMethod ("Add", new[] {typeof (object), typeof (object)});
				if (mi == null)
				{
					mi = LookupAddMethod (t, typeof (IDictionary));
				}
			}
			mi.Invoke (instance, new[] {key, item});
		}

		private MethodInfo LookupAddMethod (Type ct, Type iface)
		{
			var map = ct.GetInterfaceMap (iface);
			for (var i = 0; i < map.TargetMethods.Length; i++)
			{
				if (map.InterfaceMethods [i].Name == "Add")
				{
					return map.TargetMethods [i];
				}
			}

			return null;
		}

		public virtual object CreateInstance (object [] arguments)
		{
			ThrowIfUnknown ();
			return Activator.CreateInstance (_type.UnderlyingType, arguments);
		}

		public virtual MethodInfo GetAddMethod (XamlType contentType)
		{
			return _type == null || _type.UnderlyingType == null || _type.ItemType == null || _type.LookupCollectionKind () == XamlCollectionKind.None ? null : _type.UnderlyingType.GetMethod ("Add", new[] {contentType.UnderlyingType});
		}

		public virtual MethodInfo GetEnumeratorMethod ()
		{
			return _type.UnderlyingType == null || _type.LookupCollectionKind () == XamlCollectionKind.None ? null : _type.UnderlyingType.GetMethod ("GetEnumerator");
		}
		
		public virtual IEnumerator GetItems (object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException (nameof(instance));
			}

			return ((IEnumerable) instance).GetEnumerator ();
		}
	}
}
