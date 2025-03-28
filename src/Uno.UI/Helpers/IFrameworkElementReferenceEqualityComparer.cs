using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers
{

	/// <summary>
	/// A reference equality comparer for IFrameworkElement instances.
	/// </summary>
	/// <remarks>
	/// This comparer should be used anywhere an IFrameworkElement is used as 
	/// a Key (e.g. <see cref="Dictionary{TKey, TValue}"/>) for lookup performance.
	/// </remarks>
	internal class IFrameworkElementReferenceEqualityComparer : IEqualityComparer<IFrameworkElement>
	{
		public static readonly IFrameworkElementReferenceEqualityComparer Default = new IFrameworkElementReferenceEqualityComparer();

		private IFrameworkElementReferenceEqualityComparer() { }

		public bool Equals(IFrameworkElement left, IFrameworkElement right) => ReferenceEquals(left, right);

		public int GetHashCode(IFrameworkElement obj) => obj.GetHashCode();
	}
}
