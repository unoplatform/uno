namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class LinearEase : IEasingFunction
	{
		public static LinearEase Instance { get; } = new LinearEase();

		private LinearEase()
		{
		}

		/// <inheritdoc />
		public double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var by = finalValue - startValue;
			var currentFrame = currentTime / duration;
			var currentValue = by * currentFrame;

			return currentValue + startValue;
		}
	}
}
