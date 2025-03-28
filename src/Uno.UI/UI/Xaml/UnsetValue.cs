using System;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// This class is used for DependencyProperty.UnsetValue.
	/// </summary>
	[Windows.UI.Xaml.Data.Bindable]
	internal class UnsetValue : IEquatable<UnsetValue>
	{
		private UnsetValue()
		{
		}

		internal static UnsetValue Instance { get; } = new UnsetValue();

		public bool Equals(UnsetValue other) => !(other is null);

		public override bool Equals(object obj)
		{
			return ReferenceEquals(obj, this);
		}

		public override int GetHashCode() => 0;

		public static bool operator ==(UnsetValue left, UnsetValue right) => Equals(left, right);

		public static bool operator !=(UnsetValue left, UnsetValue right) => !Equals(left, right);
	}
}
