namespace Windows.UI.Xaml.Media
{
	partial class Geometry
	{

		public static implicit operator Geometry(string data)
		{
			return new GeometryData(data);
		}
	}
}
