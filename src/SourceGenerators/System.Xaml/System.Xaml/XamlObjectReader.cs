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
using System.Linq;
using System.Windows.Markup;

//
// This implementation can be compiled under .NET, using different namespace
// (Mono.Xaml). To compile it into a usable library, use the following compile
// optons and sources:
//
//	dmcs -d:DOTNET -t:library -r:Uno.Xaml.dll \
//		Uno.Xaml/XamlObjectReader.cs \
//		Uno.Xaml/XamlObjectNodeIterator.cs \
//		Uno.Xaml/XamlNode.cs \
//		Uno.Xaml/PrefixLookup.cs \
//		Uno.Xaml/ValueSerializerContext.cs \
//		Uno.Xaml/TypeExtensionMethods.cs
//
// (At least it should compile as of the revision that this comment is added.)
//
// Adding Test/Uno.Xaml/TestedTypes.cs might also be useful to examine this
// reader behavior under .NET and see where bugs are alive.
//

#if DOTNET
namespace Mono.Xaml
#else
namespace Uno.Xaml
#endif
{
	public class XamlObjectReader : XamlReader
	{
		public XamlObjectReader (object instance)
			: this (instance, new XamlSchemaContext (null, null), null)
		{
		}

		public XamlObjectReader (object instance, XamlObjectReaderSettings settings)
			: this (instance, new XamlSchemaContext (null, null), settings)
		{
		}

		public XamlObjectReader (object instance, XamlSchemaContext schemaContext)
			: this (instance, schemaContext, null)
		{
		}

		public XamlObjectReader (object instance, XamlSchemaContext schemaContext, XamlObjectReaderSettings settings)
		{
			if (schemaContext == null)
			{
				throw new ArgumentNullException (nameof(schemaContext));
			}
			// FIXME: special case? or can it be generalized? In .NET, For Type instance Instance returns TypeExtension at root StartObject, while for Array it remains to return Array.
			if (instance is Type)
			{
				instance = new TypeExtension ((Type) instance);
			}

			// See also Instance property for this weirdness.
			_rootRaw = instance;
			instance = TypeExtensionMethods.GetExtensionWrapped (instance);
			_root = instance;

			_sctx = schemaContext;
//			this.settings = settings;

			// check type validity. Note that some checks also needs done at Read() phase. (it is likely FIXME:)
			if (instance != null) {
				var type = new InstanceContext (instance).GetRawValue ().GetType ();
				if (!type.IsPublic)
				{
					throw new XamlObjectReaderException (string.Format ("instance type '{0}' must be public and non-nested.", type));
				}

				var xt = SchemaContext.GetXamlType (type);
				if (xt.ConstructionRequiresArguments && !xt.GetConstructorArguments ().Any () && xt.TypeConverter == null)
				{
					throw new XamlObjectReaderException (string.Format ("instance type '{0}' has no default constructor.", type));
				}
			}

			_valueSerializerContext = new ValueSerializerContext (new PrefixLookup (_sctx), _sctx, null);
			new XamlObjectNodeIterator (instance, _sctx, _valueSerializerContext).PrepareReading ();
		}

		private bool _isEof;
		private readonly object _root;
		private readonly object _rootRaw;

		private readonly XamlSchemaContext _sctx;
//		XamlObjectReaderSettings settings;
		private readonly IValueSerializerContext _valueSerializerContext;

		private IEnumerator<NamespaceDeclaration> _nsIterator;
		private IEnumerator<XamlNodeInfo> _nodes;

		private PrefixLookup PrefixLookup {
			get { return (PrefixLookup) _valueSerializerContext.GetService (typeof (INamespacePrefixLookup)); }
		}

		// This property value is weird.
		// - For root Type it returns TypeExtension.
		// - For root Array it returns Array.
		// - For non-root Type it returns Type.
		// - For IXmlSerializable, it does not either return the raw IXmlSerializable or interpreted XData (it just returns null).
		public virtual object Instance {
			get {
				var cur = NodeType == XamlNodeType.StartObject ? _nodes.Current.Object.GetRawValue () : null;
				return cur == _root ? _rootRaw : cur is XData ? null : cur;
			}
		}

		public override bool IsEof {
			get { return _isEof; }
		}

		public override XamlMember Member {
			get { return NodeType == XamlNodeType.StartMember ? _nodes.Current.Member.Member : null; }
		}

		public override NamespaceDeclaration Namespace {
			get { return NodeType == XamlNodeType.NamespaceDeclaration ? _nsIterator.Current : null; }
		}

		public override XamlNodeType NodeType {
			get {
				if (_isEof)
				{
					return XamlNodeType.None;
				}
				else if (_nodes != null)
				{
					return _nodes.Current.NodeType;
				}
				else if (_nsIterator != null)
				{
					return XamlNodeType.NamespaceDeclaration;
				}
				else
				{
					return XamlNodeType.None;
				}
			}
		}

		public override XamlSchemaContext SchemaContext {
			get { return _sctx; }
		}

		public override XamlType Type {
			get { return NodeType == XamlNodeType.StartObject ? _nodes.Current.Object.Type : null; }
		}

		public override object Value {
			get {
				if (NodeType != XamlNodeType.Value)
				{
					return null;
				}

				return _nodes.Current.Value;
			}
		}

		public override bool Read ()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException ("reader");
			}

			if (IsEof)
			{
				return false;
			}

			if (_nsIterator == null)
			{
				_nsIterator = PrefixLookup.Namespaces.GetEnumerator ();
			}

			if (_nsIterator.MoveNext ())
			{
				return true;
			}

			if (_nodes == null)
			{
				_nodes = new XamlObjectNodeIterator (_root, _sctx, _valueSerializerContext).GetNodes ().GetEnumerator ();
			}

			if (_nodes.MoveNext ())
			{
				return true;
			}

			if (!_isEof)
			{
				_isEof = true;
			}

			return false;
		}
	}
}
