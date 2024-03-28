namespace Windows.UI.Xaml
{
	public partial class CornerRadiusHelper
	{
		public static CornerRadius FromRadii(double topLeft, double topRight, double bottomRight, double bottomLeft)
			=> new CornerRadius(topLeft: topLeft, topRight: topRight, bottomRight: bottomRight, bottomLeft: bottomLeft);

		public static CornerRadius FromUniformRadius(double uniformRadius)
			=> new CornerRadius(uniformRadius: uniformRadius);
	}
}
