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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using Uno.Xaml.Schema;

#if DOTNET
namespace Mono.Xaml
#else
namespace Uno.Xaml
#endif
{
	internal abstract class XamlWriterInternalBase
	{
		public XamlWriterInternalBase (XamlSchemaContext schemaContext, XamlWriterStateManager manager)
		{
			_sctx = schemaContext;
			_manager = manager;
			var p = new PrefixLookup (_sctx) { IsCollectingNamespaces = true }; // it does not raise unknown namespace error.
			ServiceProvider = new ValueSerializerContext (p, schemaContext, AmbientProvider);
		}

		private readonly XamlSchemaContext _sctx;
		private readonly XamlWriterStateManager _manager;

		internal IValueSerializerContext ServiceProvider;

		internal ObjectState RootState;
		internal Stack<ObjectState> ObjectStates = new Stack<ObjectState> ();
		internal PrefixLookup PrefixLookup {
			get { return (PrefixLookup) ServiceProvider.GetService (typeof (INamespacePrefixLookup)); }
		}

		private List<NamespaceDeclaration> Namespaces {
			get { return PrefixLookup.Namespaces; }
		}

		internal virtual IAmbientProvider AmbientProvider {
			get { return null; }
		}

		internal class ObjectState
		{
			public XamlType Type;
			public bool IsGetObject;
			public int PositionalParameterIndex = -1;

			public string FactoryMethod;
			public object Value;
			public object KeyValue;
			public List<MemberAndValue> WrittenProperties = new List<MemberAndValue> ();
			public bool IsInstantiated;
			public bool IsXamlWriterCreated; // affects AfterProperties() calls.
		}
		
		internal class MemberAndValue
		{
			public MemberAndValue (XamlMember xm)
			{
				Member = xm;
			}

			public XamlMember Member;
			public object Value;
			public AllowedMemberLocations OccuredAs = AllowedMemberLocations.None;
		}

		public void CloseAll ()
		{
			while (ObjectStates.Count > 0) {
				switch (_manager.State) {
				case XamlWriteState.MemberDone:
				case XamlWriteState.ObjectStarted: // StartObject without member
					WriteEndObject ();
					break;
				case XamlWriteState.ValueWritten:
				case XamlWriteState.ObjectWritten:
				case XamlWriteState.MemberStarted: // StartMember without content
					_manager.OnClosingItem ();
					WriteEndMember ();
					break;
				default:
					throw new NotImplementedException (_manager.State.ToString ()); // there shouldn't be anything though
				}
			}
		}

		internal string GetPrefix (string ns)
		{
			foreach (var nd in Namespaces)
			{
				if (nd.Namespace == ns)
				{
					return nd.Prefix;
				}
			}

			return null;
		}

		protected MemberAndValue CurrentMemberState {
			get { return ObjectStates.Count > 0 ? ObjectStates.Peek ().WrittenProperties.LastOrDefault () : null; }
		}

		protected XamlMember CurrentMember {
			get {
				var mv = CurrentMemberState;
				return mv?.Member;
			}
		}

		public void WriteGetObject ()
		{
			_manager.GetObject ();

			var xm = CurrentMember;

			var state = new ObjectState () {Type = xm.Type, IsGetObject = true};

			ObjectStates.Push (state);

			OnWriteGetObject ();
		}

		public void WriteNamespace (NamespaceDeclaration namespaceDeclaration)
		{
			if (namespaceDeclaration == null)
			{
				throw new ArgumentNullException (nameof(namespaceDeclaration));
			}

			_manager.Namespace ();

			Namespaces.Add (namespaceDeclaration);
			OnWriteNamespace (namespaceDeclaration);
		}

		public void WriteStartObject (XamlType xamlType)
		{
			if (xamlType == null)
			{
				throw new ArgumentNullException (nameof(xamlType));
			}

			_manager.StartObject ();

			var cstate = new ObjectState () {Type = xamlType};
			ObjectStates.Push (cstate);

			OnWriteStartObject ();
		}
		
		public void WriteValue (object value)
		{
			_manager.Value ();

			OnWriteValue (value);
		}
		
		public void WriteStartMember (XamlMember property)
		{
			if (property == null)
			{
				throw new ArgumentNullException (nameof(property));
			}

			_manager.StartMember ();
			if (property == XamlLanguage.PositionalParameters)
			{
				// this is an exception that indicates the state manager to accept more than values within this member.
				_manager.AcceptMultipleValues = true;
			}

			var state = ObjectStates.Peek ();
			var wpl = state.WrittenProperties;
			if (wpl.Any (wp => wp.Member == property))
			{
				throw new XamlDuplicateMemberException (string.Format ("Property '{0}' is already set to this '{1}' object", property, ObjectStates.Peek ().Type));
			}

			wpl.Add (new MemberAndValue (property));
			if (property == XamlLanguage.PositionalParameters)
			{
				state.PositionalParameterIndex = 0;
			}

			OnWriteStartMember (property);
		}
		
		public void WriteEndObject ()
		{
			_manager.EndObject (ObjectStates.Count > 1);

			OnWriteEndObject ();

			ObjectStates.Pop ();
		}

		public void WriteEndMember ()
		{
			_manager.EndMember ();

			OnWriteEndMember ();
			
			var state = ObjectStates.Peek ();
			if (CurrentMember == XamlLanguage.PositionalParameters) {
				_manager.AcceptMultipleValues = false;
				state.PositionalParameterIndex = -1;
			}
		}

		protected abstract void OnWriteEndObject ();

		protected abstract void OnWriteEndMember ();

		protected abstract void OnWriteStartObject ();

		protected abstract void OnWriteGetObject ();

		protected abstract void OnWriteStartMember (XamlMember xm);

		protected abstract void OnWriteValue (object value);

		protected abstract void OnWriteNamespace (NamespaceDeclaration nd);
		
		protected string GetValueString (XamlMember xm, object value)
		{
			// change XamlXmlReader too if we change here.
			if ((value as string) == string.Empty) // FIXME: there could be some escape syntax.
			{
				return "\"\"";
			}

			if (value is string)
			{
				return (string) value;
			}

			var xt = value == null ? XamlLanguage.Null : _sctx.GetXamlType (value.GetType ());
			var vs = xm.ValueSerializer ?? xt.ValueSerializer;
			if (vs != null)
			{
				return vs.ConverterInstance.ConvertToString (value, ServiceProvider);
			}
			else
			{
				throw new XamlXmlWriterException (string.Format ("Value type is '{0}' but it must be either string or any type that is convertible to string indicated by TypeConverterAttribute.", value?.GetType ()));
			}
		}
	}
}
