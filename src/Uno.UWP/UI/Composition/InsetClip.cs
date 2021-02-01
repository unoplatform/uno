#nullable enable

namespace Windows.UI.Composition
{
	public  partial class InsetClip : CompositionClip
	{
		public InsetClip(Compositor compositor) : base(compositor)
		{

		}

		public float TopInset { get; set; }

		public  float RightInset { get; set; }

		public  float LeftInset { get; set; }

		public  float BottomInset { get; set; }
	}
}
