namespace Windows.ApplicationModel.Activation
{
	public partial class ToastNotificationActivatedEventArgs : IToastNotificationActivatedEventArgs, IActivatedEventArgs, IActivatedEventArgsWithUser, IApplicationViewActivatedEventArgs
	{
		public string Argument { get; internal set; }
		public ActivationKind Kind { get => ActivationKind.ToastNotification; }
	}
}
