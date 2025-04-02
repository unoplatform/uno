namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class EmailManager
	{
#if __IOS__
		/// <summary>
		/// Allows using.
		/// </summary>
		/// <remarks>Applies to iOS only.</remarks>        
		public static bool UseMailAppAsDefaultEmailClient { get; set; } = true;
#endif
	}
}