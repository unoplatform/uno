#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	public  partial class CompositionAnimation
	{
		internal CompositionAnimation() : base(null)
		{

		}
		internal CompositionAnimation(Compositor compositor) : base(compositor)
		{
		}
	}
}
