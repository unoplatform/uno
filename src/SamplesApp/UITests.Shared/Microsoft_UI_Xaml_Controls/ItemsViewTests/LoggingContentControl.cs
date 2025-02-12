// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace MUXControlsTestApp.Utilities;

public sealed partial class LoggingContentControl : ContentControl
{
	public LoggingContentControl()
	{
		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::LoggingContentControl[" + GetHashCode() + "]()");
	}

	~LoggingContentControl()
	{
		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::~LoggingContentControl()");
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::MeasureOverride[" + GetHashCode() + "](availableSize: " + availableSize.Width + " x " + availableSize.Height + ")");

		Size returnedSize = base.MeasureOverride(availableSize);

		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::MeasureOverride[" + GetHashCode() + "] - returnedSize: " + returnedSize.Width + " x " + returnedSize.Height);

		return returnedSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::ArrangeOverride[" + GetHashCode() + "](availableSize: " + finalSize.Width + " x " + finalSize.Height + ")");

		Size returnedSize = base.ArrangeOverride(finalSize);

		global::System.Diagnostics.Debug.WriteLine("LoggingContentControl::ArrangeOverride[" + GetHashCode() + "] - returnedSize: " + returnedSize.Width + " x " + returnedSize.Height);

		return returnedSize;
	}
}
