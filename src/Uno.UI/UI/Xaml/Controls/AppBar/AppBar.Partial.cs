// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AppBar : ContentControl
	{
		protected virtual void GetVerticalOffsetNeededToOpenUp(out double neededOffset, out bool opensWindowed)
		{
			double verticalDelta = 0d;
			var templateSettings = TemplateSettings;

			var closedDisplayMode = ClosedDisplayMode;

			verticalDelta = closedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => templateSettings.CompactVerticalDelta,
				AppBarClosedDisplayMode.Minimal => templateSettings.MinimalVerticalDelta,
				_ => templateSettings.HiddenVerticalDelta,
			};

			neededOffset = -verticalDelta;
			opensWindowed = false;
		}
	}
}
