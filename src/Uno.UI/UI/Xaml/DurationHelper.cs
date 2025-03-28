#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	public partial class DurationHelper
	{
		public static Duration Automatic
			=> Duration.Automatic;

		public static Duration Forever
			=> Duration.Forever;

		public static int Compare(Duration duration1, Duration duration2)
			=> Duration.Compare(duration1, duration2);

		public static Duration FromTimeSpan(global::System.TimeSpan timeSpan)
			=> new Duration(timeSpan);

		public static bool GetHasTimeSpan(Duration target) => target.HasTimeSpan;

		public static Duration Add(Duration target, Duration duration)
			=> target.Add(duration);

		public static bool Equals(Duration target, Duration value)
			=> Duration.Equals(target, value);

		public static Duration Subtract(Duration target, Duration duration)
			=> target.Subtract(duration);
	}
}
