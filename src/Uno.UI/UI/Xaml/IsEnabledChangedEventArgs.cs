namespace Windows.UI.Xaml
{
	internal partial class IsEnabledChangedEventArgs
	{
		internal DependencyPropertyChangedEventArgs SourceEvent { get; set; }

		public bool OldValue => (bool)SourceEvent.OldValue;

		public bool NewValue => (bool)SourceEvent.NewValue;
	}
}
