namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides event data.
	/// </summary>
	public sealed partial class RefreshInteractionRatioChangedEventArgs
	{
		internal RefreshInteractionRatioChangedEventArgs(double interactionRatio)
		{
			InteractionRatio = interactionRatio;
		}

		/// <summary>
		/// Gets the interaction ratio value.
		/// </summary>
		public double InteractionRatio { get; }
	}
}
