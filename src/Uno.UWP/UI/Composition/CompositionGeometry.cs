namespace Windows.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		internal CompositionGeometry(Compositor compositor) : base(compositor)
		{

		}

		internal CompositionGeometry()
		{

		}

		public float TrimStart { get; set; }

		public float TrimOffset { get; set; }

		public float TrimEnd { get; set; }
	}
}
