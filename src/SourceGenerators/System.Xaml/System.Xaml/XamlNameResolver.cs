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

namespace Uno.Xaml
{
	internal class XamlNameResolver : IXamlNameResolver, IXamlNameProvider
	{
		public XamlNameResolver ()
		{
		}
		
		public bool IsCollectingReferences { get; set; }

		internal class NamedObject
		{
			public NamedObject (string name, object value, bool fullyInitialized)
			{
				Name = name;
				Value = value;
				FullyInitialized = fullyInitialized;
			}
			public string Name { get; set; }
			public object Value { get; set; }
			public bool FullyInitialized { get; set; }
		}

		private readonly Dictionary<string,NamedObject> _objects = new Dictionary<string,NamedObject> ();
		private readonly List<object> _referenced = new List<object> ();

		[MonoTodo]
		public bool IsFixupTokenAvailable {
			get { throw new NotImplementedException (); }
		}

		public event EventHandler OnNameScopeInitializationComplete;

		internal void NameScopeInitializationCompleted (object sender)
		{
			OnNameScopeInitializationComplete?.Invoke (sender, EventArgs.Empty);

			_objects.Clear ();
		}

		private int _savedCount, _savedReferencedCount;
		public void Save ()
		{
			if (_savedCount != 0)
			{
				throw new Exception ();
			}

			_savedCount = _objects.Count;
			_savedReferencedCount = _referenced.Count;
		}
		public void Restore ()
		{
			while (_savedCount < _objects.Count)
			{
				_objects.Remove (_objects.Last ().Key);
			}

			_referenced.Remove (_objects.Last ().Key);
			_savedCount = 0;
			_referenced.RemoveRange (_savedReferencedCount, _referenced.Count - _savedReferencedCount);
			_savedReferencedCount = 0;
		}

		internal void SetNamedObject (string name, object value, bool fullyInitialized)
		{
			if (value == null)
			{
				throw new ArgumentNullException (nameof(value));
			}

			_objects [name] = new NamedObject (name, value, fullyInitialized);
		}
		
		internal bool Contains (string name)
		{
			return _objects.ContainsKey (name);
		}
		
		public string GetName (object value)
		{
			foreach (var no in _objects.Values)
			{
				if (ReferenceEquals (no.Value, value))
				{
					return no.Name;
				}
			}

			return null;
		}

		internal void SaveAsReferenced (object val)
		{
			_referenced.Add (val);
		}
		
		internal string GetReferencedName (object val)
		{
			if (!_referenced.Contains (val))
			{
				return null;
			}

			return GetName (val);
		}
		
		public object GetFixupToken (IEnumerable<string> names)
		{
			return new NameFixupRequired (names, false);
		}

		public object GetFixupToken (IEnumerable<string> names, bool canAssignDirectly)
		{
			return new NameFixupRequired (names, canAssignDirectly);
		}

		public IEnumerable<KeyValuePair<string, object>> GetAllNamesAndValuesInScope ()
		{
			foreach (var pair in _objects)
			{
				yield return new KeyValuePair<string,object> (pair.Key, pair.Value.Value);
			}
		}

		public object Resolve (string name)
		{
			return _objects.TryGetValue (name, out var ret) ? ret.Value : null;
		}

		public object Resolve (string name, out bool isFullyInitialized)
		{
			if (_objects.TryGetValue (name, out var ret)) {
				isFullyInitialized = ret.FullyInitialized;
				return ret.Value;
			} else {
				isFullyInitialized = false;
				return null;
			}
		}
	}
	
	internal class NameFixupRequired
	{
		public NameFixupRequired (IEnumerable<string> names, bool canAssignDirectly)
		{
			CanAssignDirectly = canAssignDirectly;
			Names = names.ToArray ();
		}
		
		public XamlType ParentType { get; set; }
		public XamlMember ParentMember { get; set; }
		public object ParentValue { get; set; }

		public bool CanAssignDirectly { get; set; }
		public IList<string> Names { get; set; }
	}
}

