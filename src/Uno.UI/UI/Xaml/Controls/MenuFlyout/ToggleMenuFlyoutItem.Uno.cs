using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class ToggleMenuFlyoutItem
{
	/// <summary>
	/// Test hook used to determine if the item is a toggle.
	/// </summary>
	internal override bool HasToggle() => true;
}
