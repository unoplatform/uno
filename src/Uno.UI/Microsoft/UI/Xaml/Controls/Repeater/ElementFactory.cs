// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ElementFactory : IElementFactoryShim
	{
		#region IElementFactory

		public UIElement GetElement(ElementFactoryGetArgs args)
			=> GetElementCore(args);

		public void RecycleElement(ElementFactoryRecycleArgs args)
			=> RecycleElementCore(args);
		#endregion

		#region IElementFactoryOverrides

		protected virtual UIElement GetElementCore(ElementFactoryGetArgs args)
			=> throw new NotImplementedException();

		protected virtual void RecycleElementCore(ElementFactoryRecycleArgs args)
			=> throw new NotImplementedException();
		#endregion
	}
}
