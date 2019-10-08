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

// To use this under .NET, compile sources as:
//
//	dmcs -d:DOTNET -r:Uno.Xaml -debug Uno.Xaml/XamlXmlWriter.cs Uno.Xaml/XamlWriterInternalBase.cs Uno.Xaml/TypeExtensionMethods.cs Uno.Xaml/XamlWriterStateManager.cs Uno.Xaml/XamlNameResolver.cs Uno.Xaml/PrefixLookup.cs Uno.Xaml/ValueSerializerContext.cs ../../build/common/MonoTODOAttribute.cs Test/Uno.Xaml/TestedTypes.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Xaml.Schema;
using System.Xml;

//
// XamlWriter expects write operations in premised orders.
// The most basic one is:
//
//	[NamespaceDeclaration]* -> StartObject -> [ StartMember -> Value | StartObject ... EndObject -> EndMember ]* -> EndObject
//
// For collections:
//	[NamespaceDeclaration]* -> StartObject -> (members)* -> StartMember XamlLanguage.Items -> [ StartObject ... EndObject ]* -> EndMember -> EndObject
//
// For MarkupExtension with PositionalParameters:
//
//	[NamespaceDeclaration]* -> StartObject -> StartMember XamlLanguage.PositionalParameters -> [Value]* -> EndMember -> ... -> EndObject
//

#if DOTNET
namespace Mono.Xaml
#else
namespace Uno.Xaml
#endif
{
	public class XamlXmlWriter : XamlWriter
	{
		public XamlXmlWriter (Stream stream, XamlSchemaContext schemaContext)
			: this (stream, schemaContext, null)
		{
		}
		
		public XamlXmlWriter (Stream stream, XamlSchemaContext schemaContext, XamlXmlWriterSettings settings)
			: this (XmlWriter.Create (stream), schemaContext, null)
		{
		}
		
		public XamlXmlWriter (TextWriter textWriter, XamlSchemaContext schemaContext)
			: this (XmlWriter.Create (textWriter), schemaContext, null)
		{
		}
		
		public XamlXmlWriter (TextWriter textWriter, XamlSchemaContext schemaContext, XamlXmlWriterSettings settings)
			: this (XmlWriter.Create (textWriter), schemaContext, null)
		{
		}
		
		public XamlXmlWriter (XmlWriter xmlWriter, XamlSchemaContext schemaContext)
			: this (xmlWriter, schemaContext, null)
		{
		}
		
		public XamlXmlWriter (XmlWriter xmlWriter, XamlSchemaContext schemaContext, XamlXmlWriterSettings settings)
		{
			_w = xmlWriter ?? throw new ArgumentNullException (nameof(xmlWriter));
			_sctx = schemaContext ?? throw new ArgumentNullException (nameof(schemaContext));
			Settings = settings ?? new XamlXmlWriterSettings ();
			var manager = new XamlWriterStateManager<XamlXmlWriterException, InvalidOperationException> (true);
			_intl = new XamlXmlWriterInternal (xmlWriter, _sctx, manager);
		}

		private readonly XmlWriter _w;
		private readonly XamlSchemaContext _sctx;

		private readonly XamlXmlWriterInternal _intl;

		public override XamlSchemaContext SchemaContext => _sctx;

		public XamlXmlWriterSettings Settings
		{
			get;
		}

		protected override void Dispose (bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_intl.CloseAll ();

			_w.Flush ();
			if (Settings.CloseOutput)
			{
				_w.Close ();
			}
		}

		public void Flush () => _w.Flush ();

		public override void WriteGetObject ()
		{
			_intl.WriteGetObject ();
		}

		public override void WriteNamespace (NamespaceDeclaration namespaceDeclaration)
		{
			_intl.WriteNamespace (namespaceDeclaration);
		}

		public override void WriteStartObject (XamlType type)
		{
			_intl.WriteStartObject (type);
		}
		
		public override void WriteValue (object value)
		{
			_intl.WriteValue (value);
		}
		
		public override void WriteStartMember (XamlMember property)
		{
			_intl.WriteStartMember (property);
		}
		
		public override void WriteEndObject ()
		{
			_intl.WriteEndObject ();
		}

		public override void WriteEndMember ()
		{
			_intl.WriteEndMember ();
		}
	}
	
	// specific implementation
	internal class XamlXmlWriterInternal : XamlWriterInternalBase
	{
		private const string Xmlns2000Namespace = "http://www.w3.org/2000/xmlns/";

		public XamlXmlWriterInternal (XmlWriter w, XamlSchemaContext schemaContext, XamlWriterStateManager manager)
			: base (schemaContext, manager)
		{
			_w = w;
//			this.sctx = schemaContext;
		}

		private readonly XmlWriter _w;
//		XamlSchemaContext sctx;
		
		// Here's a complication.
		// - local_nss holds namespace declarations that are written *before* current element.
		// - local_nss2 holds namespace declarations that are wrtten *after* current element.
		//   (current element == StartObject or StartMember)
		// - When the next element or content is being written, local_nss items are written *within* current element, BUT after all attribute members are written. Hence I had to preserve all those nsdecls at such late.
		// - When current *start* element is closed, then copy local_nss2 items into local_nss.
		// - When there was no children i.e. end element immediately occurs, local_nss should be written at this stage too, and local_nss2 are *ignored*.
		private readonly List<NamespaceDeclaration> _localNss = new List<NamespaceDeclaration> ();
		private readonly List<NamespaceDeclaration> _localNss2 = new List<NamespaceDeclaration> ();
		private bool _insideToplevelPositionalParameter;
		private bool _insideAttributeObject;

		protected override void OnWriteEndObject ()
		{
			WritePendingStartMember (XamlNodeType.EndObject);

			var state = ObjectStates.Count > 0 ? ObjectStates.Peek () : null;
			if (state != null && state.IsGetObject) {
				// do nothing
				state.IsGetObject = false;
			} else if (_w.WriteState == WriteState.Attribute) {
				_w.WriteString ("}");
				_insideAttributeObject = false;
			} else {
				WritePendingNamespaces ();
				_w.WriteEndElement ();
			}
		}

		protected override void OnWriteEndMember ()
		{
			WritePendingStartMember (XamlNodeType.EndMember);

			var member = CurrentMember;
			if (member == XamlLanguage.Initialization)
			{
				return;
			}

			if (member == XamlLanguage.Items)
			{
				return;
			}

			if (member.Type.IsCollection && member.IsReadOnly)
			{
				return;
			}

			if (member.DeclaringType != null && member == member.DeclaringType.ContentProperty)
			{
				return;
			}

			if (_insideToplevelPositionalParameter) {
				_w.WriteEndAttribute ();
				_insideToplevelPositionalParameter = false;
			} else if (_insideAttributeObject) {
				// do nothing. It didn't open this attribute.
			} else {
				switch (CurrentMemberState.OccuredAs) {
				case AllowedMemberLocations.Attribute:
					_w.WriteEndAttribute ();
					break;
				case AllowedMemberLocations.MemberElement:
					WritePendingNamespaces ();
					_w.WriteEndElement ();
					break;
				// case (AllowedMemberLocations) 0xFF:
				//	do nothing
				}
			}
		}
		
		protected override void OnWriteStartObject ()
		{
			var tmp = ObjectStates.Pop ();
			var xamlType = tmp.Type;

			WritePendingStartMember (XamlNodeType.StartObject);

			var ns = xamlType.PreferredXamlNamespace;
			var prefix = GetPrefix (ns); // null prefix is not rejected...

			if (_w.WriteState == WriteState.Attribute) {
				// MarkupExtension
				_w.WriteString ("{");
				if (!string.IsNullOrEmpty (prefix)) {
					_w.WriteString (prefix);
					_w.WriteString (":");
				}
				var name = ns == XamlLanguage.Xaml2006Namespace ? xamlType.GetInternalXmlName () : xamlType.Name;
				_w.WriteString (name);
				// space between type and first member (if any).
				if (xamlType.IsMarkupExtension && xamlType.GetSortedConstructorArguments ().GetEnumerator ().MoveNext ())
				{
					_w.WriteString (" ");
				}
			} else {
				WritePendingNamespaces ();
				_w.WriteStartElement (prefix, xamlType.GetInternalXmlName (), xamlType.PreferredXamlNamespace);
				var l = xamlType.TypeArguments;
				if (l != null) {
					_w.WriteStartAttribute ("x", "TypeArguments", XamlLanguage.Xaml2006Namespace);
					for (var i = 0; i < l.Count; i++) {
						if (i > 0)
						{
							_w.WriteString (", ");
						}

						_w.WriteString (new XamlTypeName (l [i]).ToString (PrefixLookup));
					}
					_w.WriteEndAttribute ();
				}
			}

			ObjectStates.Push (tmp);
		}

		protected override void OnWriteGetObject ()
		{
			if (ObjectStates.Count > 1) {
				var state = ObjectStates.Pop ();

				if (!CurrentMember.Type.IsCollection)
				{
					throw new InvalidOperationException (string.Format ("WriteGetObject method can be invoked only when current member '{0}' is of collection type", CurrentMember));
				}

				ObjectStates.Push (state);
			}

			WritePendingStartMember (XamlNodeType.GetObject);
		}

		private void WritePendingStartMember (XamlNodeType nodeType)
		{
			var cm = CurrentMemberState;
			if (cm == null || cm.OccuredAs != AllowedMemberLocations.Any)
			{
				return;
			}

			var state = ObjectStates.Peek ();
			if (nodeType == XamlNodeType.Value)
			{
				OnWriteStartMemberAttribute (state.Type, CurrentMember);
			}
			else
			{
				OnWriteStartMemberElement (state.Type, CurrentMember);
			}
		}
		
		protected override void OnWriteStartMember (XamlMember member)
		{
			if (member == XamlLanguage.Initialization)
			{
				return;
			}

			if (member == XamlLanguage.Items)
			{
				return;
			}

			if (member.Type.IsCollection && member.IsReadOnly)
			{
				return;
			}

			if (member.DeclaringType != null && member == member.DeclaringType.ContentProperty)
			{
				return;
			}

			var state = ObjectStates.Peek ();
			
			// Top-level positional parameters are somehow special.
			// - If it has only one parameter, it is written as an
			//   attribute using the actual argument's member name.
			// - If there are more than one, then it is an error at
			//   the second constructor argument.
			// (Here "top-level" means an object that involves
			//  StartObject i.e. the root or a collection item.)
			using (var posprms = member == XamlLanguage.PositionalParameters && IsAtTopLevelObject() && ObjectStates.Peek().Type.HasPositionalParameters(ServiceProvider) ? state.Type.GetSortedConstructorArguments().GetEnumerator() : null)
			{
				if (posprms != null) {
					posprms.MoveNext ();
					var arg = posprms.Current;
					_w.WriteStartAttribute (arg.GetInternalXmlName ());
					_insideToplevelPositionalParameter = true;
				}
				else if (_w.WriteState == WriteState.Attribute)
				{
					_insideAttributeObject = true;
				}

				if (_w.WriteState == WriteState.Attribute) {
					if (state.PositionalParameterIndex >= 0)
					{
						return;
					}

					_w.WriteString (" ");
					_w.WriteString (member.Name);
					_w.WriteString ("=");
				}
				else if (member == XamlLanguage.PositionalParameters && posprms == null && state.Type.GetSortedConstructorArguments ().All (m => m == state.Type.ContentProperty)) // PositionalParameters and ContentProperty, excluding such cases that it is already processed above (as attribute).
				{
					OnWriteStartMemberContent (state.Type, member);
				}
				else {
					switch (IsAttribute (state.Type, member)) {
						case AllowedMemberLocations.Attribute:
							OnWriteStartMemberAttribute (state.Type, member);
							break;
						case AllowedMemberLocations.MemberElement:
							OnWriteStartMemberElement (state.Type, member);
							break;
						default: // otherwise - pending output
							CurrentMemberState.OccuredAs = AllowedMemberLocations.Any; // differentiate from .None
							break;
					}
				}
			}
		}

		private bool IsAtTopLevelObject ()
		{
			if (ObjectStates.Count == 1)
			{
				return true;
			}

			var tmp = ObjectStates.Pop ();
			var parentMember = ObjectStates.Peek ().WrittenProperties.LastOrDefault ()?.Member;
			ObjectStates.Push (tmp);

			return parentMember == XamlLanguage.Items;
		}

		private AllowedMemberLocations IsAttribute (XamlType ownerType, XamlMember xm)
		{
			var xt = ownerType;
			var mt = xm.Type;
			if (xm == XamlLanguage.Key) {
				var tmp = ObjectStates.Pop ();
				mt = ObjectStates.Peek ().Type.KeyType;
				ObjectStates.Push (tmp);
			}

			if (xm == XamlLanguage.Initialization)
			{
				return AllowedMemberLocations.MemberElement;
			}

			if (mt.HasPositionalParameters (ServiceProvider))
			{
				return AllowedMemberLocations.Attribute;
			}

			if (_w.WriteState == WriteState.Content)
			{
				return AllowedMemberLocations.MemberElement;
			}

			if (xt.IsDictionary && xm != XamlLanguage.Key)
			{
				return AllowedMemberLocations.MemberElement; // as each item holds a key.
			}

			var xd = xm as XamlDirective;
			if (xd != null && (xd.AllowedLocation & AllowedMemberLocations.Attribute) == 0)
			{
				return AllowedMemberLocations.MemberElement;
			}

			// surprisingly, WriteNamespace() can affect this.
			if (_localNss2.Count > 0)
			{
				return AllowedMemberLocations.MemberElement;
			}

			// Somehow such a "stranger" is processed as an element.
			if (xd == null && !xt.GetAllMembers ().Contains (xm))
			{
				return AllowedMemberLocations.None;
			}

			if (xm.IsContentValue (ServiceProvider) || mt.IsContentValue (ServiceProvider))
			{
				return AllowedMemberLocations.Attribute;
			}

			return AllowedMemberLocations.MemberElement;
		}

		private void OnWriteStartMemberElement (XamlType xt, XamlMember xm)
		{
			CurrentMemberState.OccuredAs = AllowedMemberLocations.MemberElement;
			string prefix = GetPrefix (xm.PreferredXamlNamespace);
			string name = xm.IsDirective ? xm.Name : string.Concat (xt.GetInternalXmlName (), ".", xm.Name);
			WritePendingNamespaces ();
			_w.WriteStartElement (prefix, name, xm.PreferredXamlNamespace);
		}

		private void OnWriteStartMemberAttribute (XamlType xt, XamlMember xm)
		{
			CurrentMemberState.OccuredAs = AllowedMemberLocations.Attribute;
			var name = xm.GetInternalXmlName ();
			if (xt.PreferredXamlNamespace == xm.PreferredXamlNamespace &&
			    !(xm is XamlDirective)) // e.g. x:Key inside x:Int should not be written as Key.
			{
				_w.WriteStartAttribute (name);
			}
			else {
				var prefix = GetPrefix (xm.PreferredXamlNamespace);
				_w.WriteStartAttribute (prefix, name, xm.PreferredXamlNamespace);
			}
		}

		private void OnWriteStartMemberContent (XamlType xt, XamlMember member)
		{
			// FIXME: well, it is sorta nasty, would be better to define different enum.
			CurrentMemberState.OccuredAs = (AllowedMemberLocations) 0xFF;
		}

		protected override void OnWriteValue (object value)
		{
			if (value != null && !(value is string))
			{
				throw new ArgumentException ("Non-string value cannot be written.");
			}

			var xm = CurrentMember;
			WritePendingStartMember (XamlNodeType.Value);

			if (_w.WriteState != WriteState.Attribute)
			{
				WritePendingNamespaces ();
			}

			string s = GetValueString (xm, value);

			// It looks like a bad practice, but since .NET disables
			// indent around XData, I assume they do this, instead
			// of examining valid Text value by creating XmlReader
			// and call XmlWriter.WriteNode().
			if (xm.DeclaringType == XamlLanguage.XData && xm == XamlLanguage.XData.GetMember ("Text")) {
				_w.WriteRaw (s);
				return;
			}

			var state = ObjectStates.Peek ();
			switch (state.PositionalParameterIndex) {
			case -1:
				break;
			case 0:
				state.PositionalParameterIndex++;
				break;
			default:
				if (_insideToplevelPositionalParameter)
				{
					throw new XamlXmlWriterException (string.Format ("The XAML reader input has more than one positional parameter values within a top-level object {0} because it tries to write all of the argument values as an attribute value of the first argument. While XamlObjectReader can read such an object, XamlXmlWriter cannot write such an object to XML.", state.Type));
				}

				state.PositionalParameterIndex++;
				_w.WriteString (", ");
				break;
			}
			_w.WriteString (s);
		}

		protected override void OnWriteNamespace (NamespaceDeclaration nd)
		{
			_localNss2.Add (nd);
		}

		private void WritePendingNamespaces ()
		{
			foreach (var nd in _localNss) {
				if (string.IsNullOrEmpty (nd.Prefix))
				{
					_w.WriteAttributeString ("xmlns", nd.Namespace);
				}
				else
				{
					_w.WriteAttributeString ("xmlns", nd.Prefix, Xmlns2000Namespace, nd.Namespace);
				}
			}
			_localNss.Clear ();

			_localNss.AddRange (_localNss2);
			_localNss2.Clear ();
		}
	}

#if DOTNET
	internal static class TypeExtensionMethods2
	{
		static TypeExtensionMethods2 ()
		{
			SpecialNames = new SpecialTypeNameList ();
		}

		public static string GetInternalXmlName (this XamlType type)
		{
			if (type.IsMarkupExtension && type.Name.EndsWith ("Extension", StringComparison.Ordinal))
				return type.Name.Substring (0, type.Name.Length - 9);
			var stn = SpecialNames.FirstOrDefault (s => s.Type == type);
			return stn != null ? stn.Name : type.Name;
		}

		// FIXME: I'm not sure if these "special names" should be resolved like this. I couldn't find any rule so far.
		internal static readonly SpecialTypeNameList SpecialNames;

		internal class SpecialTypeNameList : List<SpecialTypeName>
		{
			internal SpecialTypeNameList ()
			{
				Add (new SpecialTypeName ("Member", XamlLanguage.Member));
				Add (new SpecialTypeName ("Property", XamlLanguage.Property));
			}

			public XamlType Find (string name, string ns)
			{
				if (ns != XamlLanguage.Xaml2006Namespace)
					return null;
				var stn = this.FirstOrDefault (s => s.Name == name);
				return stn != null ? stn.Type : null;
			}
		}

		internal class SpecialTypeName
		{
			public SpecialTypeName (string name, XamlType type)
			{
				Name = name;
				Type = type;
			}
			
			public string Name { get; private set; }
			public XamlType Type { get; private set; }
		}
	}
#endif
}
