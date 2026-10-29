namespace Microsoft.UI.Xaml.Input
{
	public partial class CanExecuteRequestedEventArgs
	{
		internal CanExecuteRequestedEventArgs()
		{
		}

		public bool CanExecute
		{
			get; set;
		}

		public object Parameter
		{
			get; internal set;
		}
	}
}
