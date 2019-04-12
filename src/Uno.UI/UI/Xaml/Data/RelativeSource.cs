namespace Windows.UI.Xaml.Data
{
	public partial class RelativeSource/* : DependencyObject*/
	{
		public RelativeSource()
		{

		}

		public RelativeSource(RelativeSourceMode mode)
		{
			Mode = mode;
		}

		/// <summary>
		/// Gets or sets a value that describes the location of the binding source relative to the position of the binding target.
		/// </summary>
		/// <value>The mode.</value>
		public RelativeSourceMode Mode { get; set; }

		/// <summary>
		/// Represents a TemplatedParent RelativeSource.
		/// </summary>
		public static readonly RelativeSource TemplatedParent = new RelativeSource(RelativeSourceMode.TemplatedParent);

		/// <summary>
		/// Represents a TemplatedParent Self.
		/// </summary>
		internal static readonly RelativeSource Self = new RelativeSource(RelativeSourceMode.Self);

		public override int GetHashCode()
		{
			return Mode.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is RelativeSource other)
			{
				return other.Mode == Mode;
			}

			return false;
		}
	}
}

