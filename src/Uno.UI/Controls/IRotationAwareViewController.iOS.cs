using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Controls
{
	/// <summary>
	/// An instance of a <see cref="UIKit.UIViewController"/> that can toggle its ability to auto-rotate.
	/// </summary>
	public interface IRotationAwareViewController
	{
		/// <summary>
		/// Gets or sets the bool value indicating whether the view controller's contents should auto-rotate.
		/// <remarks>This property should be returned from your view controller's <c><see cref="M:UIKit.UIViewController.ShouldAutorotate()"/></c> overridden method</remarks>
		/// </summary>
		bool CanAutorotate { get; set; }
	}
}
