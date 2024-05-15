namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides information about an event that occurs when the app is activated
/// because a user tapped on the body of a toast notification or performed
/// an action inside a toast notification.
/// </summary>
public partial interface IToastNotificationActivatedEventArgs : IActivatedEventArgs
{
	/// <summary>
	/// Gets the arguments that the app can retrieve after it is activated
	/// through an interactive toast notification.
	/// </summary>
	string Argument { get; }
}
