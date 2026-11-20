// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ElementFactory : global::Microsoft.UI.Xaml.IElementFactory, IElementFactoryShim
	{
		// Spare args for translating between the two args families. Taken while in use, so a re-entrant
		// call allocates its own instead of clearing the caller's.
		private ElementFactoryGetArgs? _shimGetArgs;
		private ElementFactoryRecycleArgs? _shimRecycleArgs;
		private global::Microsoft.UI.Xaml.ElementFactoryGetArgs? _getArgs;
		private global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs? _recycleArgs;

		#region IElementFactory

		public UIElement GetElement(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
			=> GetElementCore(args);

		public void RecycleElement(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
			=> RecycleElementCore(args);
		#endregion

		#region IElementFactoryOverrides

		protected virtual UIElement GetElementCore(ElementFactoryGetArgs args)
			=> throw new NotImplementedException();

		protected virtual void RecycleElementCore(ElementFactoryRecycleArgs args)
			=> throw new NotImplementedException();
		#endregion

		// Uno: in WinUI, GetElementCore/RecycleElementCore take the Microsoft.UI.Xaml args. Uno's own factories
		// override these; by default they hand the call to the public overrides above, which take the
		// IElementFactoryShim args.
		// TODO Uno: remove with IElementFactoryShim, moving the public overrides to the Microsoft.UI.Xaml args.
		private protected virtual UIElement GetElementCore(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
		{
			var shimArgs = _shimGetArgs ?? new ElementFactoryGetArgs();
			_shimGetArgs = null;
			try
			{
				shimArgs.Data = args.Data;
				shimArgs.Parent = args.Parent;
				shimArgs.Index = args.Index;
				return GetElementCore(shimArgs);
			}
			finally
			{
				shimArgs.Data = null;
				shimArgs.Parent = null;
				_shimGetArgs = shimArgs;
			}
		}

		private protected virtual void RecycleElementCore(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
		{
			var shimArgs = _shimRecycleArgs ?? new ElementFactoryRecycleArgs();
			_shimRecycleArgs = null;
			try
			{
				shimArgs.Element = args.Element;
				shimArgs.Parent = args.Parent;
				RecycleElementCore(shimArgs);
			}
			finally
			{
				shimArgs.Element = null;
				shimArgs.Parent = null;
				_shimRecycleArgs = shimArgs;
			}
		}

		// TODO Uno: remove with IElementFactoryShim.
		#region IElementFactoryShim

		public UIElement GetElement(ElementFactoryGetArgs args)
		{
			var xamlArgs = _getArgs ?? new global::Microsoft.UI.Xaml.ElementFactoryGetArgs();
			_getArgs = null;
			try
			{
				xamlArgs.Data = args.Data;
				xamlArgs.Parent = args.Parent;
				xamlArgs.Index = args.Index;
				return GetElementCore(xamlArgs);
			}
			finally
			{
				xamlArgs.Data = null;
				xamlArgs.Parent = null;
				_getArgs = xamlArgs;
			}
		}

		public void RecycleElement(ElementFactoryRecycleArgs args)
		{
			var xamlArgs = _recycleArgs ?? new global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs();
			_recycleArgs = null;
			try
			{
				xamlArgs.Element = args.Element;
				xamlArgs.Parent = args.Parent;
				RecycleElementCore(xamlArgs);
			}
			finally
			{
				xamlArgs.Element = null;
				xamlArgs.Parent = null;
				_recycleArgs = xamlArgs;
			}
		}
		#endregion
	}
}
