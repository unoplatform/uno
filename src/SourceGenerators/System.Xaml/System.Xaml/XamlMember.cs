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
using System.Reflection;
using System.Windows.Markup;
using Uno.Xaml.Schema;

namespace Uno.Xaml
{
	public class XamlMember : IEquatable<XamlMember>
	{
		public XamlMember (EventInfo eventInfo, XamlSchemaContext schemaContext)
			: this (eventInfo, schemaContext, null)
		{
		}

		public XamlMember (EventInfo eventInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
			: this (schemaContext, invoker)
		{
			if (eventInfo == null)
			{
				throw new ArgumentNullException (nameof(eventInfo));
			}

			Name = eventInfo.Name;
			_underlyingMember = eventInfo;
			DeclaringType = schemaContext.GetXamlType (eventInfo.DeclaringType);
			_targetType = DeclaringType;
			UnderlyingSetter = eventInfo.GetAddMethod ();
			_isEvent = true;
		}

		public XamlMember (PropertyInfo propertyInfo, XamlSchemaContext schemaContext)
			: this (propertyInfo, schemaContext, null)
		{
		}

		public XamlMember (PropertyInfo propertyInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
			: this (schemaContext, invoker)
		{
			if (propertyInfo == null)
			{
				throw new ArgumentNullException (nameof(propertyInfo));
			}

			Name = propertyInfo.Name;
			_underlyingMember = propertyInfo;
			DeclaringType = schemaContext.GetXamlType (propertyInfo.DeclaringType);
			_targetType = DeclaringType;
			UnderlyingGetter = propertyInfo.GetGetMethod (true);
			UnderlyingSetter = propertyInfo.GetSetMethod (true);
		}

		public XamlMember (string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext)
			: this (attachableEventName, adder, schemaContext, null)
		{
		}

		public XamlMember (string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
			: this (schemaContext, invoker)
		{
			if (adder == null)
			{
				throw new ArgumentNullException (nameof(adder));
			}

			Name = attachableEventName ?? throw new ArgumentNullException (nameof(attachableEventName));
			VerifyAdderSetter (adder);
			_underlyingMember = adder;
			DeclaringType = schemaContext.GetXamlType (adder.DeclaringType);
			_targetType = schemaContext.GetXamlType (typeof (object));
			UnderlyingSetter = adder;
			_isEvent = true;
			IsAttachable = true;
		}

		public XamlMember (string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
			: this (attachablePropertyName, getter, setter, schemaContext, null)
		{
		}

		public XamlMember (string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
			: this (schemaContext, invoker)
		{
			if (getter == null && setter == null)
			{
				throw new ArgumentNullException (nameof(getter), "Either property getter or setter must be non-null.");
			}

			Name = attachablePropertyName ?? throw new ArgumentNullException (nameof(attachablePropertyName));
			VerifyGetter (getter);
			VerifyAdderSetter (setter);
			_underlyingMember = getter ?? setter;
			DeclaringType = schemaContext.GetXamlType (_underlyingMember.DeclaringType);
			_targetType = schemaContext.GetXamlType (typeof (object));
			UnderlyingGetter = getter;
			UnderlyingSetter = setter;
			IsAttachable = true;
		}

		public XamlMember (string name, XamlType declaringType, bool isAttachable)
		{
			if (declaringType == null)
			{
				throw new ArgumentNullException (nameof(declaringType));
			}

			Name = name ?? throw new ArgumentNullException (nameof(name));
			_invoker = new XamlMemberInvoker (this);
			_context = declaringType.SchemaContext;
			DeclaringType = declaringType;
			_targetType = DeclaringType;
			IsAttachable = isAttachable;
		}

		internal static XamlMember FromUnknown(string name, string ns, XamlType declaringType)
		{
			var m = new XamlMember(false, ns, name);

			m._context = XamlLanguage.UnknownContent.Type.SchemaContext;
			m.DeclaringType = declaringType;
			m.IsAttachable = false;

			return m;
		}

		private XamlMember(XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
		{
			_context = schemaContext ?? throw new ArgumentNullException (nameof(schemaContext));
			_invoker = invoker ?? new XamlMemberInvoker (this);
		}

		internal XamlMember (bool isDirective, string ns, string name)
		{
			_directiveNs = ns;
			Name = name;
			IsDirective = isDirective;
		}

		private XamlType _type;
		private readonly XamlType _targetType;
		private readonly MemberInfo _underlyingMember;
		private MethodInfo _underlyingGetter, _underlyingSetter;
		private XamlSchemaContext _context;
		private readonly XamlMemberInvoker _invoker;
		private readonly bool _isEvent;
		private readonly bool _isPredefinedDirective = XamlLanguage.InitializingDirectives;
		private readonly string _directiveNs;

		internal MethodInfo UnderlyingGetter {
			get { return LookupUnderlyingGetter (); }
			private set { _underlyingGetter = value; }
		}
		internal MethodInfo UnderlyingSetter {
			get { return LookupUnderlyingSetter (); }
			private set { _underlyingSetter = value; }
		}

		public XamlType DeclaringType { get; private set; }
		public string Name { get; }

		public string PreferredXamlNamespace {
			get { return _directiveNs ?? (DeclaringType == null ? null : DeclaringType.PreferredXamlNamespace); }
		}
		
		public DesignerSerializationVisibility SerializationVisibility {
			get {
				var c= GetCustomAttributeProvider ();
				var a = c?.GetCustomAttribute<DesignerSerializationVisibilityAttribute> (false);
				return a?.Visibility ?? DesignerSerializationVisibility.Visible;
			}
		}

		public bool IsAttachable
		{
			get;
			private set;
		}

		public bool IsDirective
		{
			get;
		}

		public bool IsNameValid {
			get { return XamlLanguage.IsValidXamlName (Name); }
		}

		public XamlValueConverter<XamlDeferringLoader> DeferringLoader {
			get { return LookupDeferringLoader (); }
		}

		private static readonly XamlMember [] EmptyMembers = new XamlMember [0];
		
		public IList<XamlMember> DependsOn {
			get { return LookupDependsOn () ?? EmptyMembers; }
		}

		public XamlMemberInvoker Invoker {
			get { return LookupInvoker (); }
		}
		public bool IsAmbient {
			get { return LookupIsAmbient (); }
		}
		public bool IsEvent {
			get { return LookupIsEvent (); }
		}
		public bool IsReadOnly {
			get { return LookupIsReadOnly (); }
		}
		public bool IsReadPublic {
			get { return LookupIsReadPublic (); }
		}
		public bool IsUnknown {
			get { return LookupIsUnknown (); }
		}
		public bool IsWriteOnly {
			get { return LookupIsWriteOnly (); }
		}
		public bool IsWritePublic {
			get { return LookupIsWritePublic (); }
		}
		public XamlType TargetType {
			get { return LookupTargetType (); }
		}
		public XamlType Type {
			get { return LookupType (); }
		}
		public XamlValueConverter<TypeConverter> TypeConverter {
			get { return LookupTypeConverter (); }
		}
		public MemberInfo UnderlyingMember {
			get { return LookupUnderlyingMember (); }
		}
		public XamlValueConverter<ValueSerializer> ValueSerializer {
			get { return LookupValueSerializer (); }
		}

		public static bool operator == (XamlMember xamlMember1, XamlMember xamlMember2)
		{
			return IsNull (xamlMember1) ? IsNull (xamlMember2) : xamlMember1.Equals (xamlMember2);
		}

		private static bool IsNull (XamlMember a)
		{
			return ReferenceEquals (a, null);
		}

		public static bool operator != (XamlMember xamlMember1, XamlMember xamlMember2)
		{
			return !(xamlMember1 == xamlMember2);
		}
		
		public override bool Equals (object obj)
		{
			var x = obj as XamlMember;
			return Equals (x);
		}
		
		public bool Equals (XamlMember other)
		{
			// this should be in general correct; XamlMembers are almost not comparable.
			if (ReferenceEquals (this, other))
			{
				return true;
			}
			// It does not compare XamlSchemaContext.
			return !IsNull (other) &&
				_underlyingMember == other._underlyingMember &&
				_underlyingGetter == other._underlyingGetter &&
				_underlyingSetter == other._underlyingSetter &&
				Name == other.Name &&
				PreferredXamlNamespace == other.PreferredXamlNamespace &&
				_directiveNs == other._directiveNs &&
				IsAttachable == other.IsAttachable;
		}

		public override int GetHashCode ()
		{
			return ToString ().GetHashCode (); // should in general work.
		}

		[MonoTodo ("there are some patterns that return different kind of value: e.g. List<int>.Capacity")]
		public override string ToString ()
		{
			if (IsAttachable || string.IsNullOrEmpty(PreferredXamlNamespace))
			{
				if (DeclaringType == null)
				{
					return Name;
				}
				else
				{
					return string.Concat(DeclaringType.UnderlyingType.FullName, ".", Name);
				}
			}
			else
			{
				var s = string.Concat("{", PreferredXamlNamespace, "}", DeclaringType.Name, ".", Name);

				return s;
			}
		}

		public virtual IList<string> GetXamlNamespaces ()
		{
			throw new NotImplementedException ();
		}

		// lookups

		internal ICustomAttributeProvider GetCustomAttributeProvider ()
		{
			return LookupCustomAttributeProvider ();
		}

		protected virtual ICustomAttributeProvider LookupCustomAttributeProvider ()
		{
			return UnderlyingMember;
		}

		protected virtual XamlValueConverter<XamlDeferringLoader> LookupDeferringLoader ()
		{
			// FIXME: use XamlDeferLoadAttribute.
			return null;
		}

		private static readonly XamlMember [] EmptyList = new XamlMember [0];

		protected virtual IList<XamlMember> LookupDependsOn ()
		{
			return EmptyList;
		}

		protected virtual XamlMemberInvoker LookupInvoker ()
		{
			return _invoker;
		}
		protected virtual bool LookupIsAmbient ()
		{
			var t = Type != null ? Type.UnderlyingType : null;
			return t != null && t.GetCustomAttributes (typeof (AmbientAttribute), false).Length > 0;
		}

		protected virtual bool LookupIsEvent ()
		{
			return _isEvent;
		}

		protected virtual bool LookupIsReadOnly ()
		{
			return UnderlyingGetter != null && UnderlyingSetter == null;
		}
		protected virtual bool LookupIsReadPublic ()
		{
			if (_underlyingMember == null)
			{
				return true;
			}

			if (UnderlyingGetter != null)
			{
				return UnderlyingGetter.IsPublic;
			}

			return false;
		}

		protected virtual bool LookupIsUnknown ()
		{
			return _underlyingMember == null;
		}

		protected virtual bool LookupIsWriteOnly ()
		{
			var pi = _underlyingMember as PropertyInfo;
			if (pi != null)
			{
				return !pi.CanRead && pi.CanWrite;
			}

			return UnderlyingGetter == null && UnderlyingSetter != null;
		}

		protected virtual bool LookupIsWritePublic ()
		{
			if (_underlyingMember == null)
			{
				return true;
			}

			if (UnderlyingSetter != null)
			{
				return UnderlyingSetter.IsPublic;
			}

			return false;
		}

		protected virtual XamlType LookupTargetType ()
		{
			return _targetType;
		}

		protected virtual XamlType LookupType ()
		{
			if (_type == null)
			{
				_type = _context.GetXamlType (DoGetType ());
			}

			return _type;
		}


		private Type DoGetType ()
		{
			var pi = _underlyingMember as PropertyInfo;
			if (pi != null)
			{
				return pi.PropertyType;
			}

			var ei = _underlyingMember as EventInfo;
			if (ei != null)
			{
				return ei.EventHandlerType;
			}

			if (_underlyingSetter != null)
			{
				return _underlyingSetter.GetParameters () [1].ParameterType;
			}

			if (_underlyingGetter != null)
			{
				return _underlyingGetter.GetParameters () [0].ParameterType;
			}

			return typeof (object);
		}

		protected virtual XamlValueConverter<TypeConverter> LookupTypeConverter ()
		{
			var t = Type.UnderlyingType;
			if (t == null)
			{
				return null;
			}

			if (t == typeof (object)) // it is different from XamlType.LookupTypeConverter().
			{
				return null;
			}

			var a = GetCustomAttributeProvider ();
			var ca = a?.GetCustomAttribute<TypeConverterAttribute> (false);
			if (ca != null)
			{
				return _context.GetValueConverter<TypeConverter> (System.Type.GetType (ca.ConverterTypeName), Type);
			}

			return Type.TypeConverter;
		}

		protected virtual MethodInfo LookupUnderlyingGetter ()
		{
			return _underlyingGetter;
		}

		protected virtual MemberInfo LookupUnderlyingMember ()
		{
			return _underlyingMember;
		}

		protected virtual MethodInfo LookupUnderlyingSetter ()
		{
			return _underlyingSetter;
		}

		protected virtual XamlValueConverter<ValueSerializer> LookupValueSerializer ()
		{
			if (_isPredefinedDirective) // FIXME: this is likely a hack.
			{
				return null;
			}

			if (Type == null)
			{
				return null;
			}

			return XamlType.LookupValueSerializer (Type, LookupCustomAttributeProvider ()) ?? Type.ValueSerializer;
		}

		private void VerifyGetter (MethodInfo method)
		{
			if (method == null)
			{
				return;
			}

			if (method.GetParameters ().Length != 1 || method.ReturnType == typeof (void))
			{
				throw new ArgumentException (string.Format ("Property getter for {0} must have exactly one argument and must have non-void return type.", Name));
			}
		}

		private void VerifyAdderSetter (MethodInfo method)
		{
			if (method == null)
			{
				return;
			}

			if (method.GetParameters ().Length != 2)
			{
				throw new ArgumentException (string.Format ("Property getter or event adder for {0} must have exactly one argument and must have non-void return type.", Name));
			}
		}
	}
}
