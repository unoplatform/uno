// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// InitializationType.h

namespace Uno.UI.Xaml.Core
{
	internal enum InitializationType
	{
		/// <summary>
		/// This initialization type  is used when
		/// none of the other explicit types
		/// make sense. In a practical sense this is
		/// for initialization of non-main view in
		/// foreground apps.
		/// </summary>
		Normal,

		/// <summary>
		/// This initialization type is used when
		/// the initialization is for main view in
		/// the foreground app.
		/// </summary>
		MainView,

		/// <summary>
		/// This initialization type is used when
		/// the initialization is for xbf.
		/// </summary>
		Xbf,

		/// <summary>
		/// This initialization type is used when
		///  the initialization is for background tasks.
		/// </summary>
		BackgroundTask,

		/// <summary>
		/// Xaml core is started only to host islands (StartOnCurrentThread API).
		/// This is also the mode used by WinUI Desktop apps.
		/// </summary>
		IslandsOnly,

		/// <summary>
		/// This Initialization type is used when
		/// we shutdown the core in between test runs
		/// and we need to get the core back to an
		/// initialized state from an idle state
		/// </summary>
		FromIdle,
	};
}
