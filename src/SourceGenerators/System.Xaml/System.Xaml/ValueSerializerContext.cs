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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using Uno.Xaml.Schema;

namespace Uno.Xaml
{
	internal class ValueSerializerContext : IValueSerializerContext, IXamlSchemaContextProvider
	{
		private readonly XamlNameResolver _nameResolver = new XamlNameResolver ();
		private readonly XamlTypeResolver _typeResolver;
		private readonly NamespaceResolver _namespaceResolver;
		private readonly PrefixLookup _prefixLookup;
		private readonly XamlSchemaContext _sctx;
		private readonly IAmbientProvider _ambientProvider;

		public ValueSerializerContext (PrefixLookup prefixLookup, XamlSchemaContext schemaContext, IAmbientProvider ambientProvider)
		{
			if (schemaContext == null)
			{
				throw new ArgumentNullException (nameof(schemaContext));
			}

			_prefixLookup = prefixLookup ?? throw new ArgumentNullException (nameof(prefixLookup));
			_namespaceResolver = new NamespaceResolver (_prefixLookup.Namespaces);
			_typeResolver = new XamlTypeResolver (_namespaceResolver, schemaContext);
			_sctx = schemaContext;
			_ambientProvider = ambientProvider;
		}

		public object GetService (Type serviceType)
		{
			if (serviceType == typeof (INamespacePrefixLookup))
			{
				return _prefixLookup;
			}

			if (serviceType == typeof (IXamlNamespaceResolver))
			{
				return _namespaceResolver;
			}

			if (serviceType == typeof (IXamlNameResolver))
			{
				return _nameResolver;
			}

			if (serviceType == typeof (IXamlNameProvider))
			{
				return _nameResolver;
			}

			if (serviceType == typeof (IXamlTypeResolver))
			{
				return _typeResolver;
			}

			if (serviceType == typeof (IAmbientProvider))
			{
				return _ambientProvider;
			}

			if (serviceType == typeof (IXamlSchemaContextProvider))
			{
				return this;
			}

			return null;
		}
		
		XamlSchemaContext IXamlSchemaContextProvider.SchemaContext {
			get { return _sctx; }
		}
		
		public IContainer Container {
			get { throw new NotImplementedException (); }
		}
		public object Instance {
			get { throw new NotImplementedException (); }
		}
		public PropertyDescriptor PropertyDescriptor {
			get { throw new NotImplementedException (); }
		}
		public void OnComponentChanged ()
		{
			throw new NotImplementedException ();
		}
		public bool OnComponentChanging ()
		{
			throw new NotImplementedException ();
		}
		public ValueSerializer GetValueSerializerFor (PropertyDescriptor descriptor)
		{
			throw new NotImplementedException ();
		}
		public ValueSerializer GetValueSerializerFor (Type type)
		{
			throw new NotImplementedException ();
		}
	}

	internal class XamlTypeResolver : IXamlTypeResolver
	{
		private readonly NamespaceResolver _nsResolver;
		private readonly XamlSchemaContext _schemaContext;

		public XamlTypeResolver (NamespaceResolver namespaceResolver, XamlSchemaContext schemaContext)
		{
			_nsResolver = namespaceResolver;
			_schemaContext = schemaContext;
		}

		public Type Resolve (string typeName)
		{
			var tn = XamlTypeName.Parse (typeName, _nsResolver);
			var xt = _schemaContext.GetXamlType (tn);
			return xt != null ? xt.UnderlyingType : null;
		}
	}

	internal class NamespaceResolver : IXamlNamespaceResolver
	{
		public NamespaceResolver (IList<NamespaceDeclaration> source)
		{
			_source = source;
		}

		private readonly IList<NamespaceDeclaration> _source;
	
		public string GetNamespace (string prefix)
		{
			foreach (var nsd in _source)
			{
				if (nsd.Prefix == prefix)
				{
					return nsd.Namespace;
				}
			}

			return null;
		}
	
		public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes ()
		{
			return _source;
		}
	}

	internal class AmbientProvider : IAmbientProvider
	{
		private readonly List<AmbientPropertyValue> _values = new List<AmbientPropertyValue> ();
		private readonly Stack<AmbientPropertyValue> _liveStack = new Stack<AmbientPropertyValue> ();

		public void Push (AmbientPropertyValue v)
		{
			_liveStack.Push (v);
			_values.Add (v);
		}

		public void Pop ()
		{
			_liveStack.Pop ();
		}

		public IEnumerable<object> GetAllAmbientValues (params XamlType [] types)
		{
			return GetAllAmbientValues (null, false, types);
		}
		
		public IEnumerable<AmbientPropertyValue> GetAllAmbientValues (IEnumerable<XamlType> ceilingTypes, params XamlMember [] properties)
		{
			return GetAllAmbientValues (ceilingTypes, false, null, properties);
		}
		
		public IEnumerable<AmbientPropertyValue> GetAllAmbientValues (IEnumerable<XamlType> ceilingTypes, bool searchLiveStackOnly, IEnumerable<XamlType> types, params XamlMember [] properties)
		{
			return DoGetAllAmbientValues (ceilingTypes, searchLiveStackOnly, types, properties).ToList ();
		}

		private IEnumerable<AmbientPropertyValue> DoGetAllAmbientValues (IEnumerable<XamlType> ceilingTypes, bool searchLiveStackOnly, IEnumerable<XamlType> types, params XamlMember [] properties)
		{
			if (searchLiveStackOnly) {
				if (_liveStack.Count > 0) {
					// pop, call recursively, then push back.
					var p = _liveStack.Pop ();
					if (p.RetrievedProperty != null && ceilingTypes != null && ceilingTypes.Contains (p.RetrievedProperty.Type))
					{
						yield break;
					}

					if (DoesAmbientPropertyApply (p, types, properties))
					{
						yield return p;
					}

					foreach (var i in GetAllAmbientValues (ceilingTypes, searchLiveStackOnly, types, properties))
					{
						yield return i;
					}

					_liveStack.Push (p);
				}
			} else {
				// FIXME: does ceilingTypes matter?
				foreach (var p in _values)
				{
					if (DoesAmbientPropertyApply (p, types, properties))
					{
						yield return p;
					}
				}
			}
		}

		private bool DoesAmbientPropertyApply (AmbientPropertyValue p, IEnumerable<XamlType> types, params XamlMember [] properties)
		{
			if (types == null || !types.Any () || types.Any (xt => xt.UnderlyingType != null && xt.UnderlyingType.IsInstanceOfType (p.Value)))
			{
				if (properties == null || !properties.Any () || properties.Contains (p.RetrievedProperty))
				{
					return true;
				}
			}

			return false;
		}
		
		public object GetFirstAmbientValue (params XamlType [] types)
		{
			foreach (var obj in GetAllAmbientValues (types))
			{
				return obj;
			}

			return null;
		}
		
		public AmbientPropertyValue GetFirstAmbientValue (IEnumerable<XamlType> ceilingTypes, params XamlMember [] properties)
		{
			foreach (var obj in GetAllAmbientValues (ceilingTypes, properties))
			{
				return obj;
			}

			return null;
		}
	}
}
