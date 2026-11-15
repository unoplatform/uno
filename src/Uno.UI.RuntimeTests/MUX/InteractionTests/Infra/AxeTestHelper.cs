// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/MUXControls.Test/AxeTestHelper.cs, commit fee1e8acc

namespace MUXTestInfra.Shared.Infra
{
	public class AxeTestHelper
	{
		// Uno: there is no Axe.Windows scanner in process, so accessibility scans are reported as inconclusive.
		public static void TestForAxeIssues()
		{
			Assert.Inconclusive("Axe accessibility scans are not available for Uno runtime tests.");
		}
	}
}
