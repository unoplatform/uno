using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial struct ManipulationDelta 
	{
		internal ManipulationDelta(
			Point translation,
			float scale,
			float rotation,
			float expansion)
		{
			Translation = translation;
			Scale = scale;
			Rotation = rotation;
			Expansion = expansion;
		}

		public Point Translation;
		public float Scale;
		public float Rotation;
		public float Expansion;
	}
}
