using Uno.UITest;

namespace SamplesApp.UITests
{
	public partial class AppRect : IAppRect
	{
		public AppRect(float x, float y, float width, float height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public float Width { get; }
		public float Height { get; }
		public float X { get; }
		public float Y { get; }
		public float CenterX => Width / 2f + X;
		public float CenterY => Height / 2f + Y;
		public float Right => X + Width;
		public float Bottom => Y + Height;
	}
}
