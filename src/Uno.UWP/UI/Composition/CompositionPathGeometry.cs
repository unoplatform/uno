namespace Windows.UI.Composition
{
	public partial class CompositionPathGeometry : CompositionGeometry
	{
		internal CompositionPathGeometry(Compositor compositor, CompositionPath path = null) : base(compositor)
		{
			Path = path;
		}

		public CompositionPath Path { get; set; }
	}
}
