using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A dependency property comparer that assumes that instances are unique and that the hash code will never change.
	/// </summary>
	internal class DependencyPropertyComparer : IEqualityComparer<DependencyProperty>, IEqualityComparer
	{
		/// <summary>
		/// Gets the default instance of DependencyPropertyComparer. This class has no state and cannot be created.
		/// </summary>
		public static readonly DependencyPropertyComparer Default = new DependencyPropertyComparer();

		private DependencyPropertyComparer()
		{
		}

		public bool Equals(DependencyProperty x, DependencyProperty y) => object.ReferenceEquals(x, y);

		public int GetHashCode(DependencyProperty obj) => obj.CachedHashCode;

		bool IEqualityComparer.Equals(object x, object y) => object.ReferenceEquals(x, y);
		int IEqualityComparer.GetHashCode(object obj) => obj is DependencyProperty dp ? dp.CachedHashCode : 0;
	}
}
