namespace Microsoft.UI.Xaml.Controls
{
	public partial class TimePickerSelector : ContentControl
	{
		public TimePickerSelector()
		{
			DefaultStyleKey = typeof(TimePickerSelector);

			InitPartial();
		}

		partial void InitPartial();
	}
}
