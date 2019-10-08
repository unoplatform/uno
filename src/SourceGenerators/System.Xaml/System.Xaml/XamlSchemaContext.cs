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
using System.Reflection;
using System.Windows.Markup;
using Uno.Xaml.Schema;

using Pair = System.Collections.Generic.KeyValuePair<string,string>;

namespace Uno.Xaml
{
	// This type caches assembly attribute search results. To do this,
	// it registers AssemblyLoaded event on CurrentDomain when it should
	// reflect dynamic in-scope assemblies.
	// It should be released at finalizer.
	public class XamlSchemaContext
	{
		public XamlSchemaContext ()
			: this (null, null)
		{
		}

		public XamlSchemaContext (IEnumerable<Assembly> referenceAssemblies)
			: this (referenceAssemblies, null)
		{
		}

		public XamlSchemaContext (XamlSchemaContextSettings settings)
			: this (null, settings)
		{
		}

		public XamlSchemaContext (IEnumerable<Assembly> referenceAssemblies, XamlSchemaContextSettings settings)
		{
			if (referenceAssemblies != null)
			{
				ReferenceAssemblies = new List<Assembly> (referenceAssemblies);
			}
			else
			{
				AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
			}

			if (settings == null)
			{
				return;
			}

			FullyQualifyAssemblyNamesInClrNamespaces = settings.FullyQualifyAssemblyNamesInClrNamespaces;
			SupportMarkupExtensionsWithDuplicateArity = settings.SupportMarkupExtensionsWithDuplicateArity;
		}

		~XamlSchemaContext ()
		{
			if (ReferenceAssemblies == null)
			{
				AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
			}
		}

		// assembly attribute caches
		private Dictionary<string,string> _xamlNss;
		private Dictionary<string,string> _prefixes;
		private Dictionary<string,string> _compatNss;
		private Dictionary<string,List<XamlType>> _allXamlTypes;
		private readonly XamlType [] _emptyXamlTypes = new XamlType [0];
		private readonly List<XamlType> _runTimeTypes = new List<XamlType> ();
		private readonly object _gate = new object();

		public bool FullyQualifyAssemblyNamesInClrNamespaces { get; }

		public IList<Assembly> ReferenceAssemblies
		{
			get;
		}

		private IEnumerable<Assembly> AssembliesInScope {
			get { return ReferenceAssemblies ?? AppDomain.CurrentDomain.GetAssemblies (); }
		}

		public bool SupportMarkupExtensionsWithDuplicateArity { get; }

		internal string GetXamlNamespace (string clrNamespace)
		{
			lock (_gate)
			{
				if (clrNamespace == null) // could happen on nested generic type (see bug #680385-comment#4). Not sure if null is correct though.
				{
					return null;
				}

				if (_xamlNss == null) // fill it first
				{
					GetAllXamlNamespaces();
				}

				return _xamlNss.TryGetValue(clrNamespace, out var ret) ? ret : null;
			}
		}

		public virtual IEnumerable<string> GetAllXamlNamespaces ()
		{
			lock (_gate)
			{
				if (_xamlNss == null)
				{
					_xamlNss = new Dictionary<string, string>();
					foreach (var ass in AssembliesInScope)
					{
						FillXamlNamespaces(ass);
					}
				}
				return _xamlNss.Values.Distinct();
			}
		}

		public virtual ICollection<XamlType> GetAllXamlTypes (string xamlNamespace)
		{
			lock (_gate)
			{
				if (xamlNamespace == null)
				{
					throw new ArgumentNullException(nameof(xamlNamespace));
				}

				if (_allXamlTypes == null)
				{
					_allXamlTypes = new Dictionary<string, List<XamlType>>();
					foreach (var ass in AssembliesInScope)
					{
						FillAllXamlTypes(ass);
					}
				}

				if (_allXamlTypes.TryGetValue(xamlNamespace, out var l))
				{
					return l;
				}
				else
				{
					return _emptyXamlTypes;
				}
			}
		}

		public virtual string GetPreferredPrefix (string xmlns)
		{
			lock (_gate)
			{
				if (xmlns == null)
				{
					throw new ArgumentNullException(nameof(xmlns));
				}

				if (xmlns == XamlLanguage.Xaml2006Namespace)
				{
					return "x";
				}

				if (_prefixes == null)
				{
					_prefixes = new Dictionary<string, string>();
					foreach (var ass in AssembliesInScope)
					{
						FillPrefixes(ass);
					}
				}

				return _prefixes.TryGetValue(xmlns, out var ret) ? ret : "p"; // default
			}
		}

		protected internal XamlValueConverter<TConverterBase> GetValueConverter<TConverterBase> (Type converterType, XamlType targetType)
			where TConverterBase : class
		{
			return new XamlValueConverter<TConverterBase> (converterType, targetType);
		}

		private readonly Dictionary<Pair,XamlDirective> _xamlDirectives = new Dictionary<Pair,XamlDirective> ();
		
		public virtual XamlDirective GetXamlDirective (string xamlNamespace, string name)
		{
			lock (_gate)
			{
				var p = new Pair(xamlNamespace, name);
				if (!_xamlDirectives.TryGetValue(p, out var t))
				{
					t = new XamlDirective(xamlNamespace, name);
					_xamlDirectives.Add(p, t);
				}
				return t;
			}
		}
		
		public virtual XamlType GetXamlType (Type type)
		{
			lock (_gate)
			{
				var xt = _runTimeTypes.FirstOrDefault(t => t.UnderlyingType == type);
				if (xt == null)
				{
					foreach (var ns in GetAllXamlNamespaces())
					{
						if ((xt = GetAllXamlTypes(ns).FirstOrDefault(t => t.UnderlyingType == type)) != null)
						{
							break;
						}
					}
				}

				if (xt == null)
				{
					xt = new XamlType(type, this);
					_runTimeTypes.Add(xt);
				}
				return xt;
			}
		}
		
		public XamlType GetXamlType (XamlTypeName xamlTypeName)
		{
			lock (_gate)
			{
				if (xamlTypeName == null)
				{
					throw new ArgumentNullException(nameof(xamlTypeName));
				}

				var n = xamlTypeName;
				if (n.TypeArguments.Count == 0) // non-generic
				{
					return GetXamlType(n.Namespace, n.Name);
				}

				// generic
				XamlType[] typeArgs = new XamlType[n.TypeArguments.Count];
				for (int i = 0; i < typeArgs.Length; i++)
				{
					typeArgs[i] = GetXamlType(n.TypeArguments[i]);
				}

				return GetXamlType(n.Namespace, n.Name, typeArgs);
			}
		}
		
		protected internal virtual XamlType GetXamlType (string xamlNamespace, string name, params XamlType [] typeArguments)
		{
			lock (_gate)
			{
				if (TryGetCompatibleXamlNamespace(xamlNamespace, out var dummy))
				{
					xamlNamespace = dummy;
				}

				XamlType ret;
				if (xamlNamespace == XamlLanguage.Xaml2006Namespace)
				{
					ret = XamlLanguage.SpecialNames.Find(name, xamlNamespace);
					if (ret == null)
					{
						ret = XamlLanguage.AllTypes.FirstOrDefault(t => TypeMatches(t, xamlNamespace, name, typeArguments));
					}

					if (ret != null)
					{
						return ret;
					}
				}
				ret = _runTimeTypes.FirstOrDefault(t => TypeMatches(t, xamlNamespace, name, typeArguments));
				if (ret == null)
				{
					ret = GetAllXamlTypes(xamlNamespace).FirstOrDefault(t => TypeMatches(t, xamlNamespace, name, typeArguments));
				}

				if (ReferenceAssemblies == null)
				{
					var type = ResolveXamlTypeName(xamlNamespace, name, typeArguments);
					if (type != null)
					{
						ret = GetXamlType(type);
					}
				}

				// If the type was not found, it just returns null.
				return ret;
			}
		}

		private bool TypeMatches (XamlType t, string ns, string name, XamlType [] typeArgs)
		{
			if (t.PreferredXamlNamespace == ns && t.Name == name && t.TypeArguments.ListEquals (typeArgs))
			{
				return true;
			}

			if (t.IsMarkupExtension)
			{
				return t.PreferredXamlNamespace == ns && t.Name.Substring (0, Math.Max(0, t.Name.Length - 9)) == name && t.TypeArguments.ListEquals (typeArgs);
			}
			else
			{
				return false;
			}
		}

		protected internal virtual Assembly OnAssemblyResolve (string assemblyName)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return Assembly.LoadWithPartialName (assemblyName);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public virtual bool TryGetCompatibleXamlNamespace (string xamlNamespace, out string compatibleNamespace)
		{
			lock (_gate)
			{
				if (xamlNamespace == null)
				{
					throw new ArgumentNullException(nameof(xamlNamespace));
				}

				if (_compatNss == null)
				{
					_compatNss = new Dictionary<string, string>();
					foreach (var ass in AssembliesInScope)
					{
						FillCompatibilities(ass);
					}
				}
				return _compatNss.TryGetValue(xamlNamespace, out compatibleNamespace);
			}
		}

		private void OnAssemblyLoaded (object o, AssemblyLoadEventArgs e)
		{
			if (ReferenceAssemblies != null)
			{
				return; // do nothing
			}

			if (_xamlNss != null)
			{
				FillXamlNamespaces (e.LoadedAssembly);
			}

			if (_prefixes != null)
			{
				FillPrefixes (e.LoadedAssembly);
			}

			if (_compatNss != null)
			{
				FillCompatibilities (e.LoadedAssembly);
			}

			if (_allXamlTypes != null)
			{
				FillAllXamlTypes (e.LoadedAssembly);
			}
		}

		// cache updater methods
		private void FillXamlNamespaces (Assembly ass)
		{
			foreach (XmlnsDefinitionAttribute xda in ass.GetCustomAttributes (typeof (XmlnsDefinitionAttribute), false))
			{
				_xamlNss.Add (xda.ClrNamespace, xda.XmlNamespace);
			}
		}

		private void FillPrefixes (Assembly ass)
		{
			foreach (XmlnsPrefixAttribute xpa in ass.GetCustomAttributes (typeof (XmlnsPrefixAttribute), false))
			{
				_prefixes.Add (xpa.XmlNamespace, xpa.Prefix);
			}
		}

		private void FillCompatibilities (Assembly ass)
		{
			foreach (XmlnsCompatibleWithAttribute xca in ass.GetCustomAttributes (typeof (XmlnsCompatibleWithAttribute), false))
			{
				_compatNss.Add (xca.OldNamespace, xca.NewNamespace);
			}
		}

		private void FillAllXamlTypes (Assembly ass)
		{
			foreach (XmlnsDefinitionAttribute xda in ass.GetCustomAttributes (typeof (XmlnsDefinitionAttribute), false)) {
				var l = _allXamlTypes.FirstOrDefault (p => p.Key == xda.XmlNamespace).Value;
				if (l == null) {
					l = new List<XamlType> ();
					_allXamlTypes.Add (xda.XmlNamespace, l);
				}
				foreach (var t in ass.GetTypes ())
				{
					if (t.Namespace == xda.ClrNamespace)
					{
						l.Add (GetXamlType (t));
					}
				}
			}
		}

		// XamlTypeName -> Type resolution

		private static readonly int ClrNsLen = "clr-namespace:".Length;
		private static readonly int ClrAssLen = "assembly=".Length;

		private Type ResolveXamlTypeName (string xmlNamespace, string xmlLocalName, IList<XamlType> typeArguments)
		{
			string ns = xmlNamespace;
			string name = xmlLocalName;

			if (ns == XamlLanguage.Xaml2006Namespace) {
				var xt = XamlLanguage.SpecialNames.Find (name, ns);
				if (xt == null)
				{
					xt = XamlLanguage.AllTypes.FirstOrDefault (t => t.Name == xmlLocalName);
				}

				if (xt == null)
				{
					throw new FormatException (string.Format ("There is no type '{0}' in XAML namespace", name));
				}

				return xt.UnderlyingType;
			}
			else if (!ns.StartsWith ("clr-namespace:", StringComparison.Ordinal))
			{
				return null;
			}

			Type [] genArgs = null;
			if (typeArguments != null && typeArguments.Count > 0) {
				genArgs = (from t in typeArguments select t.UnderlyingType).ToArray ();
				if (genArgs.Any (t => t == null))
				{
					return null;
				}
			}

			// convert xml namespace to clr namespace and assembly
			string [] split = ns.Split (new[] { ';' });
			if (split.Length != 2 || split [0].Length < ClrNsLen || split [1].Length <= ClrAssLen)
			{
				throw new XamlParseException (string.Format ("Cannot resolve runtime namespace from XML namespace '{0}'", ns));
			}

			string tns = split [0].Substring (ClrNsLen);
			string aname = split [1].Substring (ClrAssLen);

			string taqn = GetTypeName (tns, name, genArgs);
			var ass = OnAssemblyResolve (aname);
			// MarkupExtension type could omit "Extension" part in XML name.
			var ret = ass == null ? null : ass.GetType (taqn) ?? ass.GetType (GetTypeName (tns, name + "Extension", genArgs));
			if (ret == null)
			{
				throw new XamlParseException (string.Format ("Cannot resolve runtime type from XML namespace '{0}', local name '{1}' with {2} type arguments ({3})", ns, name, typeArguments?.Count ?? 0, taqn));
			}

			return genArgs == null ? ret : ret.MakeGenericType (genArgs);
		}

		private static string GetTypeName (string tns, string name, Type [] genArgs)
		{
			string tfn = tns.Length > 0 ? tns + '.' + name : name;
			if (genArgs != null)
			{
				tfn += "`" + genArgs.Length;
			}

			return tfn;
		}
	}
}
