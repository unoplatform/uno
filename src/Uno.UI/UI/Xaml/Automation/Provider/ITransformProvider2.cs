namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Extends the ITransformProvider interface to enable Microsoft UI Automation
/// providers to expose API to support the viewport zooming functionality of a
/// control.
/// </summary>
public partial interface ITransformProvider2 : ITransformProvider
{
	/// <summary>
	/// Gets a value that indicates whether the control supports zooming of its viewport.
	/// </summary>
	bool CanZoom { get; }

	/// <summary>
	/// Gets the maximum zoom level of the element.
	/// </summary>
	double MaxZoom { get; }

	/// <summary>
	/// Gets the minimum zoom level of the element.
	/// </summary>
	double MinZoom { get; }

	/// <summary>
	/// Gets the zoom level of the control's viewport.
	/// </summary>
	double ZoomLevel { get; }

	/// <summary>
	/// Zooms the viewport of the control.
	/// </summary>
	/// <param name="zoom">The amount to zoom the viewport, specified as a percentage.
	/// The provider should zoom the viewport to the nearest supported value.</param>
	void Zoom(double zoom);

	/// <summary>
	/// Zooms the viewport of the control by the specified logical unit.
	/// </summary>
	/// <param name="zoomUnit">The logical unit by which to increase or decrease the zoom of the viewport.</param>
	void ZoomByUnit(ZoomUnit zoomUnit);
}
