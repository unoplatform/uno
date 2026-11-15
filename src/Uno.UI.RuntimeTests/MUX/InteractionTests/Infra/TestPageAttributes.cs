// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/MUXControlsTestApp/Utilities/TopLevelTestPageAttribute.cs, commit fee1e8acc
// MUX Reference controls/test/MUXControlsTestApp/Utilities/AxeScanTestPageAttribute.cs, commit fee1e8acc

using System;

namespace MUXControlsTestApp
{
	[AttributeUsage(AttributeTargets.Class)]
	public class TopLevelTestPageAttribute : Attribute
	{
		public string Name { get; set; } = "NoName";

		public string Icon { get; set; } = "DefaultIcon.png";
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class AxeScanTestPageAttribute : Attribute
	{
		public string Name { get; set; } = "NoName";
	}
}
