#nullable enable

using Windows.Foundation;

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Provides options to help identify the next element that can programmatically receive navigation focus.
	/// </summary>
	public partial class FindNextElementOptions
	{
		/// <summary>
		/// Initializes a new instance of the FindNextElementOptions class.
		/// </summary>
		public FindNextElementOptions()
		{
		}

		/// <summary>
		/// Gets or sets the focus navigation strategy used to identify
		/// the best candidate element to receive focus.
		/// </summary>
		public XYFocusNavigationStrategyOverride XYFocusNavigationStrategyOverride { get; set; }

		/// <summary>
		/// Gets or sets the object that must be the root from which to identify
		/// the next focus candidate to receive navigation focus.
		/// </summary>
		/// <remarks>In many cases UWP throws when this is null. In non-UWP targets,
		/// we use the current window as the search root in case of null.</remarks>
		public DependencyObject? SearchRoot { get; set; }

		/// <summary>
		/// Gets or sets a bounding rectangle used to identify the focus candidates
		/// most likely to receive navigation focus.
		/// </summary>
		public Rect HintRect { get; set; }

		/// <summary>
		/// Gets or sets a bounding rectangle where all overlapping navigation
		/// candidates are excluded from navigation focus.
		/// </summary>
		public Rect ExclusionRect { get; set; }

		/// <summary>
		/// Gets or sets a valud indicating whether occlusivity
		/// should be ignored in the search.
		/// </summary>
		internal bool IgnoreOcclusivity { get; set; }
	}
}
