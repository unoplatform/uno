using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial struct ManipulationDelta 
	{
		public Point Translation;
		public float Scale;
		public float Rotation;
		public float Expansion;
	}
}
