#if !__NETSTD_REFERENCE__
#nullable enable
using System;
using System.Globalization;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

using Uno.UI;
using Uno.UI.Xaml;
using static System.Math;
using static Uno.UI.LayoutHelper;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		private bool m_firedLoadingEvent;

		private readonly Size MaxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		private protected string DepthIndentation
		{
			get
			{
				if (Depth is int d)
				{
					return (Parent as FrameworkElement)?.DepthIndentation + $"-{d}>";
				}
				else
				{
					return "-?>";
				}
			}
		}

		partial void OnLoading();

		private void OnFwEltLoading()
		{
			IsLoading = true;

			OnLoading();
			OnLoadingPartial();

			void InvokeLoading()
			{
				_loading?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadingWithTry()
				{
					try
					{
						InvokeLoading();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoading failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadingWithTry();
			}
			else
			{
				InvokeLoading();
			}
		}

		private protected sealed override void OnFwEltLoaded()
		{
			OnLoadedPartial();

			void InvokeLoaded()
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoaded();
				_loaded?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadedWithTry()
				{
					try
					{
						InvokeLoaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadedWithTry();
			}
			else
			{
				InvokeLoaded();
			}
		}

		partial void OnLoadedPartial();

		private void RaiseLoadingEventIfNeeded()
		{
			if (!m_firedLoadingEvent //&&
				/*ShouldRaiseEvent(_loading)*/ /*Uno TODO: Should we skip this or not? */)
			{
				//CEventManager* pEventManager = GetContext()->GetEventManager();
				//ASSERT(pEventManager);

				//TraceFrameworkElementLoadingBegin();

				// Uno specific: WinUI only raises Loading event here.
				OnFwEltLoading();
				//pEventManager->Raise(
				//	EventHandle(KnownEventIndex::FrameworkElement_Loading),
				//	FALSE /* bRefire */,
				//	this /* pSender */,
				//	NULL /* pArgs */,
				//	TRUE /* fRaiseSync */);

				//TraceFrameworkElementLoadingEnd();

				m_firedLoadingEvent = true;
			}
		}

		internal override void EnterImpl(EnterParams @params, int depth)
		{
			var core = this.GetContext();

			//if (@params.IsLive && @params.CheckForResourceOverrides == false)
			//{
			//    var resources = GetResourcesNoCreate();

			//    if (resources is not null &&
			//        resources.HasPotentialOverrides())
			//    {
			//        @params.CheckForResourceOverrides = TRUE;
			//    }
			//}

			base.EnterImpl(@params, depth);

			////Check for focus chrome property.
			//if (@params.IsLive)
			//{
			//	if (Control.GetIsTemplateFocusTarget(this))
			//	{
			//		UpdateFocusAncestorsTarget(true /*shouldSet*/); //Add pointer to the Descendant
			//	}
			//}

			//// Walk the list of events (if any) to keep watch of loaded events.
			//if (@params.IsLive && m_pEventList is not null)
			//{
			//	CXcpList<REQUEST>::XCPListNode* pTemp = m_pEventList.GetHead();
			//	while (pTemp is not null)
			//	{
			//		REQUEST* pRequest = (REQUEST*)pTemp->m_pData;
			//		if (pRequest && pRequest->m_hEvent.index != KnownEventIndex::UnknownType_UnknownEvent)
			//		{
			//			if (pRequest->m_hEvent.index == KnownEventIndex::FrameworkElement_Loaded)
			//			{
			//				// Take note of the fact we added a loaded event to the event manager.
			//				core->KeepWatch(WATCH_LOADED_EVENTS);
			//			}
			//		}
			//		pTemp = pTemp->m_pNext;
			//	}
			//}

			// Apply style when element is live in the tree
			if (@params.IsLive)
			{
				//if (m_eImplicitStyleProvider == ImplicitStyleProvider::None)
				//{
				//	if (!GetStyle())
				//	{
				//		IFC_RETURN(ApplyStyle());
				//	}
				//}
				//else if (m_eImplicitStyleProvider == ImplicitStyleProvider::AppWhileNotInTree)
				//{
				//	IFC_RETURN(UpdateImplicitStyle(m_pImplicitStyle, null, /*bForceUpdate*/false, /*bUpdateChildren*/false));
				//}

				// ---------- Uno-specific BEGIN ----------
				// Apply active style and default style when we enter the visual tree, if they haven't been applied already.
				this.ApplyStyles();
				// ---------- Uno-specific END ----------
			}

			// Uno-specific
			ReconfigureViewportPropagation();

			m_firedLoadingEvent = false;
		}

		// UNO TODO: Not yet ported
		internal override void LeaveImpl(LeaveParams @params)
		{
			// The way this works on WinUI is that when an element enters the visual tree, all values
			// of properties that are marked with MetaDataPropertyInfoFlags::IsSparse and MetaDataPropertyInfoFlags::IsVisualTreeProperty
			// are entered as well.
			// The property we currently know it has an effect is Resources
			if (Resources is not null)
			{
				// Using ValuesInternal to avoid Enumerator boxing
				foreach (var resource in Resources.ValuesInternal)
				{
					if (resource is FrameworkElement resourceAsUIElement)
					{
						resourceAsUIElement.LeaveImpl(@params);
					}
				}
			}

			base.LeaveImpl(@params);

			ReconfigureViewportPropagation(isLeavingTree: true);
		}
	}
}
#endif
