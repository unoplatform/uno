using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml
{
	// This interface is intended to be renamed to IUIElement once the deprecated public one has been removed.
	// Note: We do not inherit from IUIElement as we don't want to implement IDisposable!
	internal interface IUIElementInternal 
	{
		/// <summary>
		/// The 'availableSize' provided for the last Measure
		/// </summary>
		/// <remarks>This is the backing flied for <see cref="LayoutInformation.GetAvailableSize"/></remarks>
		Size LastAvailableSize { get; set; }

		/// <summary>
		/// The 'return' size produced by the last Measure
		/// </summary>
		/// <remarks>This is the backing flied for the **internal** <see cref="LayoutInformation.GetDesiredSize"/></remarks>
		Size DesiredSize { get; set; }

		/// <summary>
		/// The 'finalSize' provided for the last Arrange
		/// </summary>
		/// <remarks>This is the backing flied for <see cref="LayoutInformation.GetLayoutSlot"/></remarks>
		Rect LayoutSlot { get; set; }
	}
}

