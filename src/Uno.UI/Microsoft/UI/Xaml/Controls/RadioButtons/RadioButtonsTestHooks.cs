using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls
{
	internal static class RadioButtonsTestHooks
	{
		internal static TypedEventHandler<RadioButtons, object> LayoutChanged;

		internal static void SetTestHooksEnabled(RadioButtons radioButtons, bool enabled)
		{
			if (radioButtons != null)
			{
				radioButtons.SetTestHooksEnabled(enabled);
			}
		}

		internal static void NotifyLayoutChanged(RadioButtons sender)
		{
			LayoutChanged?.Invoke(sender, null);
		}

		internal static int GetRows(RadioButtons radioButtons)
		{
			if (radioButtons != null)
			{
				return radioButtons.GetRows();
			}
			return -1;
		}

		internal static int GetColumns(RadioButtons radioButtons)
		{
			if (radioButtons != null)
			{
				return radioButtons.GetColumns();
			}
			return -1;
		}

		internal static int GetLargerColumns(RadioButtons radioButtons)
		{
			if (radioButtons != null)
			{
				return radioButtons.GetLargerColumns();
			}
			return -1;
		}
	}
}
