#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	public  partial class InsetClip : CompositionClip
	{
		public float TopInset { get; set; }

		public  float RightInset { get; set; }

		public  float LeftInset { get; set; }

		public  float BottomInset { get; set; }
	}
}
