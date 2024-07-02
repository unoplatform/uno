using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls
{
	/// <summary>
	/// Defines how a <see cref="Windows.UI.Xaml.Controls.ComboBox"/> tries to place it drop down.
	/// </summary>
	public enum DropDownPlacement
	{
		/// <summary>
		/// When opening the "drop down" the ComboBox tries to keep the selected item over the closed combo box.
		/// That means that if the selected is the last one of the list, the "drop down" will appear above.
		/// If there isn't any selected item, the "drop down" will appear centered over the combo box.
		/// </summary>
		Auto,

		/// <summary>
		/// Always try to render the drop down above the combo
		/// </summary>
		Above,

		/// <summary>
		/// Always try to render the drop down below the combo
		/// </summary>
		Below,

		/// <summary>
		/// Always try to render the drop down center over the combo
		/// </summary>
		Centered,
	}

	/// <summary>
	/// The configurations of the <see cref="Windows.UI.Xaml.Controls.ComboBox"/> specific to the Uno platform.
	/// </summary>
	public static class ComboBox
	{
		/// <summary>
		/// Backing property for the <see cref="DropDownPlacement"/> of a ComboBox. (cf. Remarks.)
		/// </summary>
		/// <remarks>
		/// This is only the preferred placement, the combo will still ensure to keep its drop down in the visual bounds of the window
		/// (When content cannot be rendered out of the current window ...)
		/// </remarks>
		public static DependencyProperty DropDownPreferredPlacementProperty { get; } = DependencyProperty.RegisterAttached(
			"DropDownPreferredPlacement",
			typeof(DropDownPlacement),
			typeof(Windows.UI.Xaml.Controls.ComboBox),
			new FrameworkPropertyMetadata(FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement));

		/// <summary>
		/// Sets the preferred <see cref="DropDownPlacement"/> of a ComboBox. (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// This is only the preferred placement, the combo will still ensure to keep its drop down in the visual bounds of the window
		/// (When content cannot be rendered out of the current window ...)
		/// </remarks>
		/// <param name="combo">The target ComboBox to configure</param>
		/// <param name="mode">The updates mode to set</param>
		public static void SetDropDownPreferredPlacement(Windows.UI.Xaml.Controls.ComboBox combo, DropDownPlacement mode)
			=> combo.SetValue(DropDownPreferredPlacementProperty, mode);

		/// <summary>
		/// Gets the <see cref="DropDownPlacement"/> of a ComboBox. (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// This is only the preferred placement, the combo will still ensure to keep its drop down in the visual bounds of the window
		/// (When content cannot be rendered out of the current window ...)
		/// </remarks>
		/// <param name="combo">The target ComboBox</param>
		/// <returns>The updates mode of the <paramref name="combo"/>.</returns>
		public static DropDownPlacement GetDropDownPreferredPlacement(Windows.UI.Xaml.Controls.ComboBox combo)
			=> (DropDownPlacement)combo.GetValue(DropDownPreferredPlacementProperty);
	}

}
