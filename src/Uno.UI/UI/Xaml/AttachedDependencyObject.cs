namespace Windows.UI.Xaml
{
	public partial class AttachedDependencyObject : DependencyObject
	{
		internal object Owner { get; }

		public AttachedDependencyObject(object owner)
		{
			InitializeBinder();

            Owner = owner;
		}
	}
}