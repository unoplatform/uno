// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Private.Controls;

internal class ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs
{
	internal ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs(
		bool isExpressionAnimationStarted, string propertyName)
	{
		IsExpressionAnimationStarted = isExpressionAnimationStarted;
		PropertyName = propertyName;
	}

	#region IScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs

    bool IsExpressionAnimationStarted { get; }
    string PropertyName { get; }

	#endregion
}
