using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	partial class ImageBrush
	{
		internal string ToCssPosition()
		{
			var x = AlignmentX switch
			{
				AlignmentX.Left => "left",
				AlignmentX.Center => "center",
				AlignmentX.Right => "right",
				_ => ""
			};

			var y = AlignmentY switch
			{
				AlignmentY.Top=> "top",
				AlignmentY.Center => "center",
				AlignmentY.Bottom => "bottom",
				_ => ""
			};

			return $"{x} {y}";
		}
		internal string ToCssBackgroundSize()
		{
			return Stretch switch
			{
				Stretch.Fill => "100% 100%",
				Stretch.None => "auto",
				Stretch.Uniform => "auto", // patch for now
				Stretch.UniformToFill => "auto", // patch for now
				_ => "auto"
			};
		}
	}
}
