namespace Windows.ApplicationModel.Activation;

public partial class ToastNotificationActivatedEventArgs : IToastNotificationActivatedEventArgs, IActivatedEventArgs, IActivatedEventArgsWithUser, IApplicationViewActivatedEventArgs
{
	public string Argument { get; }

	public ActivationKind Kind => ActivationKind.ToastNotification;

	internal ToastNotificationActivatedEventArgs(string argument) => Argument = argument;
}
