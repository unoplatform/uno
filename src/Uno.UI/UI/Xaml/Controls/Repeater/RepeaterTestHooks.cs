// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls
{
	internal partial class RepeaterTestHooks
	{
		public static event EventHandler BuildTreeCompleted;

		public static void NotifyBuildTreeCompleted()
		{
			BuildTreeCompleted?.Invoke(null, null);
		}

		// We removed index parameter from the GetElement call, which we used extensively for 
		// validation in tests. In order to avoid rewriting the tests, we keep the index internally and have 
		// a test hook to get it for validation in tests.
		/* static */
		public static int GetElementFactoryElementIndex(object getArgs)
		{
			var args = getArgs as ElementFactoryGetArgs;
			return args.Index;
		}

		/* static */
		public static object CreateRepeaterElementFactoryGetArgs()
		{
			var instance = new ElementFactoryGetArgs();
			return instance;
		}

		/* static */
		public static object CreateRepeaterElementFactoryRecycleArgs()
		{
			var instance = new ElementFactoryRecycleArgs();
			return instance;
		}

		/* static */
		public static string GetLayoutId(object layout)
		{
			if (layout is Layout instance)
			{
				return instance.LayoutId;
			}

			return "";
		}

		/* static */
		public static void SetLayoutId(object layout, string id)
		{
			if (layout is Layout instance)
			{
				instance.LayoutId = id;
			}
		}
	}
}
