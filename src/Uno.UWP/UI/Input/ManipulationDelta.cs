using Windows.Foundation;

namespace Windows.UI.Input
{
	public partial struct ManipulationDelta 
	{
		public Point Translation;
		public float Scale;
		public float Rotation;
		public float Expansion;

		/// <inheritdoc />
		public override string ToString()
			=> $"x:{Translation.X:N0};y:{Translation.Y:N0};θ:{Rotation:F2};s:{Scale:F2};e:{Expansion:F2}";
	}
}
