using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A dependency property comparer that assumes that instances are unique and that the hash code will never change.
	/// </summary>
	internal class DependencyPropertyComparer : IEqualityComparer<DependencyProperty>
	{
		/// <summary>
		/// Gets the default instance of DependencyPropertyComparer. This class has no state and cannot be created.
		/// </summary>
		public static readonly DependencyPropertyComparer Default = new DependencyPropertyComparer();

		private DependencyPropertyComparer()
		{
		}

		public bool Equals(DependencyProperty x, DependencyProperty y)
		{
			return object.ReferenceEquals(x, y);
		}

		public int GetHashCode(DependencyProperty obj)
		{
			return obj.CachedHashCode;
		}
	}
}
