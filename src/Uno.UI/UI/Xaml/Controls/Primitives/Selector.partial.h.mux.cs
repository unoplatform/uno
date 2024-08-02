using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	private bool AreCustomValuesAllowed() => m_customValuesAllowed;

	// Allows the insertion of custom values by not reverting values outside the item source.
	private bool m_customValuesAllowed;

	protected bool m_skipFocusSuggestion;
}
