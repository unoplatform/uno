// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class LayoutContext
	{
		#region ILayoutContext
		public object LayoutState
		{
			get => LayoutStateCore;
			set => LayoutStateCore = value;
		}
		#endregion

		#region ILayoutContextOverrides

		protected internal virtual object LayoutStateCore
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}
		#endregion

#if DEBUG
		public int Indent { get; set; }
#else
		public int Indent => 0;
#endif
	}
}
