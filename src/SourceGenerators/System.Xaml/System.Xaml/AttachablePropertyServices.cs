using System;
using System.Collections.Generic;

namespace Uno.Xaml
{
	public static class AttachablePropertyServices
	{
		private class Table : Dictionary<AttachableMemberIdentifier,object>
		{
		}

		private static readonly Dictionary<object,Table> Props = new Dictionary<object,Table> ();

		public static void CopyPropertiesTo (object instance, KeyValuePair<AttachableMemberIdentifier,object> [] array, int index)
		{
			if (instance == null || !Props.TryGetValue (instance, out var t))
			{
				return;
			} ((ICollection<KeyValuePair<AttachableMemberIdentifier,object>>) t).CopyTo (array, index);
		}

		public static int GetAttachedPropertyCount (object instance)
		{
			return instance != null && Props.TryGetValue (instance, out var t) ? t.Count : 0;
		}

		public static bool RemoveProperty (object instance, AttachableMemberIdentifier name)
		{
			if (name == null)
			{
				throw new ArgumentNullException (nameof(name));
			}

			return instance != null && Props.TryGetValue (instance, out var t) && t.Remove (name);
		}

		public static void SetProperty (object instance, AttachableMemberIdentifier name, object value)
		{
			if (name == null)
			{
				throw new ArgumentNullException (nameof(name));
			}

			if (!Props.TryGetValue (instance, out var t)) {
				t = new Table ();
				Props [instance] = t;
			}
			t [name] = value;
		}

		public static bool TryGetProperty (object instance, AttachableMemberIdentifier name, out object value)
		{
			if (name == null)
			{
				throw new ArgumentNullException (nameof(name));
			}

			value = null;
			return instance != null && Props.TryGetValue (instance, out var t) && t.TryGetValue (name, out value);
		}

		public static bool TryGetProperty<T> (object instance, AttachableMemberIdentifier name, out T value)
		{
			if (!TryGetProperty (instance, name, out var ret)) {
				value = default;
				return false;
			}
			value = (T) ret;
			return true;
		}
	}
}
