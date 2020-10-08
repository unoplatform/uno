namespace Windows.ApplicationModel.Activation
{

	public partial interface IToastNotificationActivatedEventArgs : IActivatedEventArgs
	{

		string Argument
		{
			get;
		}
	}
}
