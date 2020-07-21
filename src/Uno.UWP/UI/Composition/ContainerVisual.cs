#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	public partial class ContainerVisual : global::Windows.UI.Composition.Visual
	{
		internal ContainerVisual() : base(null)
		{
		}

		internal ContainerVisual(Compositor compositor) : base(compositor)
		{
			Children = new VisualCollection(compositor, this);
		}

		public VisualCollection Children { get; }
	}
}
