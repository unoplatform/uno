namespace Windows.UI.Xaml
{
	/// <summary>
	/// Precedence of an active trigger
	/// </summary>
	internal enum StateTriggerPrecedence : byte
	{
		Inactive = 0,
		CustomTrigger = 1,
		AdaptiveTrigger = 2,
	}
}
