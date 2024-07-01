// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FrameworkElement_partial.cpp

using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Xaml;
using DirectUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		private protected virtual string GetPlainText() => "";

		internal protected static string GetStringFromObject(object pObject)
		{
			// First, try IFrameworkElement
			var spFrameworkElement = pObject as FrameworkElement;
			if (spFrameworkElement != null)
			{
				return spFrameworkElement.GetPlainText();
			}

			// Try IPropertyValue
			var type = pObject.GetType();

			if (ValueConversionHelpers.CanConvertValueToString(type))
			{
				return ValueConversionHelpers.ConvertValueToString(pObject, pObject.GetType());
			}

			// Try ICustomPropertyProvider
			var spCustomPropertyProvider = pObject as ICustomPropertyProvider;
			if (spCustomPropertyProvider != null)
			{
				return spCustomPropertyProvider.GetStringRepresentation();
			}

			// Finally, Try IStringable
			var spStringable = pObject as IStringable;
			if (spStringable != null)
			{
				return spStringable.ToString();
			}

			//TODO MZ: Should default to null instead of ToString?
			return pObject.ToString() ?? null;
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		private bool IsDataContextBound()
		{
			return this.GetBindingExpression(FrameworkElement.DataContextProperty) is not null;
		}

		internal virtual void NotifyOfDataContextChange(DataContextChangedParams args)
		{
			DataContextChangedParams tempArgs = new DataContextChangedParams(args.OriginalSource, args.DataContextChangedReason);
			DataContextChangedParams argsToForward = args;
			bool propagationHandled = false;

			// Currently DependencyObjectStore knows when the DataContext changes and notifies the bindings.
			// Our current behavior is likely not matching WinUI though, and we may need to revisit it and do the same as WinUI.
			// NotifyBindingExpressions(args);

			var isDataContextBound = IsDataContextBound();

			// Avoid walking children twice when an element has a DataContext with a live {Binding}. We make an exception
			// when entering the tree, because we need all DataContexts to get re-evaluated.
			bool fIsRelevant = (/*!m_pDataContextChangedSource ||*/ !isDataContextBound || args.OriginalSource == this || args.DataContextChangedReason == DataContextChangedReason.EnteringLiveTree);
			if (fIsRelevant)
			{
				// If the DataContext property has a {Binding}, pick up the new value of our DataContext, instead of
				// continuing to pass on the DataContext from our parent.
				if (isDataContextBound && args.ResolvedNewDataContext && (args.OriginalSource != this))
				{
					var newDataContext = DataContext;
					tempArgs.NewDataContext = newDataContext;
					tempArgs.ResolvedNewDataContext = true;
					argsToForward = tempArgs;
				}

				// Raise the public DataContextChanged event if there are listeners.
				//if (ShouldRaiseEvent(DataContextChanged))
				{
					//DXamlCore* pCore = DXamlCore::GetCurrent();
					object spNewDataContext;

					if (argsToForward.ResolvedNewDataContext)
					{
						spNewDataContext = argsToForward.NewDataContext;
					}
					else
					{
						spNewDataContext = DataContext;
					}

					//IFC(pCore->GetDataContextChangedEventArgsFromPool(spNewDataContext.Get(), &spArgs));

					//IFC(GetDataContextChangedEventSourceNoRef(&pEventSource));
					//{
					//	SuspendFailFastOnStowedException suspender; // ensure we don't FailFast due to an error within Raise
					//	IFC(pEventSource->Raise(this, spArgs.Get()));
					//}

					try
					{
						propagationHandled = this.RaiseDataContextChanged(spNewDataContext);
					}
					catch { }
				}

				if (!propagationHandled)
				{
					// Propagate the notification down the visual tree
					PropagateDataContextChanged(argsToForward);

					// If there is a ToolTip registered for this element, we need to push the new DataContext to it.
					var toolTip = ToolTipService.GetToolTip(this);
					if (toolTip is not null)
					{
						if (argsToForward.ResolvedNewDataContext)
						{
							toolTip.SetValue(FrameworkElement.DataContextProperty, argsToForward.NewDataContext);
						}
						else
						{
							toolTip.SetValue(FrameworkElement.DataContextProperty, DataContext);
						}
					}
				}
			}
		}

		private void PropagateDataContextChanged(DataContextChangedParams args)
		{
			// Very simplified logic. WinUI has a lot more code here.
			foreach (var child in GetChildren())
			{
				try
				{
					(child as FrameworkElement)?.OnAncestorDataContextChanged(args);
				}
				catch { }
			}
		}

		internal void OnAncestorDataContextChanged(DataContextChangedParams args)
		{
			// Only process the change if we care about it
			var isContextChangeRelevant = IsDataContextChangeRelevant(args);
			if (!isContextChangeRelevant)
			{
				return;
			}

			NotifyOfDataContextChange(args);
		}

		private bool IsDataContextChangeRelevant(DataContextChangedParams args)
		{
			if (IsDataContextBound())
			{
				// If the data context is data bound then any changes to the
				// ancestor data context is relevant to us, the local one will be
				// set to the expression and would otherwise prevent the
				// DC from being propagated down the tree
				return true;
			}
			else if (args.DataContextChangedReason == DataContextChangedReason.NewDataContext)
			{
				var isPropertyLocal = this.IsDependencyPropertyLocallySet(DataContextProperty);

				// We only care about the DC change if the local DC is not set
				return !isPropertyLocal;
			}
			else if (args.DataContextChangedReason == DataContextChangedReason.EnteringLiveTree)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == FrameworkElement.DataContextProperty)
			{
				NotifyOfDataContextChange(new DataContextChangedParams(this, DataContextChangedReason.NewDataContext, args.NewValue));
			}
		}
#endif
	}
}
