// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MUXControlsTestApp.Utilities;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using RefreshVisualizer = Microsoft.UI.Xaml.Controls.RefreshVisualizer;
using RefreshRequestedEventArgs = Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs;
using RefreshInteractionRatioChangedEventArgs = Microsoft.UI.Xaml.Controls.RefreshInteractionRatioChangedEventArgs;
using RefreshStateChangedEventArgs = Microsoft.UI.Xaml.Controls.RefreshStateChangedEventArgs;
using IRefreshVisualizerPrivate = Microsoft.UI.Private.Controls.IRefreshVisualizerPrivate;
using IRefreshInfoProvider = Microsoft.UI.Private.Controls.IRefreshInfoProvider;
using PullToRefreshHelperTestApi = Microsoft.UI.Private.Controls.PullToRefreshHelperTestApi;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class RefreshVisualizerTests : MUXApiTestBase
	{
		TimeSpan c_MaxWaitDuration = TimeSpan.FromMilliseconds(5000);

		[TestMethod]
		public void CanInstantiate()
		{
			AutoResetEvent refreshVisualizerLoadedEvent = new AutoResetEvent(false);
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				refreshVisualizer.Loaded += (object sender, RoutedEventArgs e) =>
				{
					Log.Comment("RefreshVisualizer.Loaded event handler");
					refreshVisualizerLoadedEvent.Set();
				};
				Content = refreshVisualizer;
			});

			Log.Comment("Waiting for Loaded event");
			refreshVisualizerLoadedEvent.WaitOne(c_MaxWaitDuration);
			Log.Comment("Default UI set up");

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNotNull(Content as RefreshVisualizer);
			});
		}

		[TestMethod]
		public void RefreshPropagatesToInfoProvider()
		{
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				Compositor compositor = ElementCompositionPreview.GetElementVisual(refreshVisualizer).Compositor;
				RefreshInfoProviderImpl refreshInfoProviderImpl = new RefreshInfoProviderImpl(compositor);
				((IRefreshVisualizerPrivate)refreshVisualizer).InfoProvider = refreshInfoProviderImpl;
				Verify.AreEqual<int>(0, refreshInfoProviderImpl.OnRefreshStartedCalls);
				Verify.AreEqual<int>(0, refreshInfoProviderImpl.OnRefreshCompletedCalls);
				refreshVisualizer.RequestRefresh();
				Verify.AreEqual<int>(1, refreshInfoProviderImpl.OnRefreshStartedCalls);
				Verify.AreEqual<int>(1, refreshInfoProviderImpl.OnRefreshCompletedCalls);
			});
		}

		[TestMethod]
		public void ContentIsUpdatable()
		{
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				//The default Progress Indicator is consturcted in the OnApplyTemplate method, since that is not called in this test
				//we expect the Content to be null.
				Verify.IsNull(refreshVisualizer.Content);
				Content = refreshVisualizer;
				Content.UpdateLayout();
				Verify.IsNotNull(refreshVisualizer.Content);
				Verify.AreEqual<Symbol>(Symbol.Refresh, ((SymbolIcon)(refreshVisualizer.Content)).Symbol);
				refreshVisualizer.Content = new SymbolIcon(Symbol.Cancel);
				Verify.AreEqual<Symbol>(Symbol.Cancel, ((SymbolIcon)(refreshVisualizer.Content)).Symbol);
			});
		}

		[TestMethod]
		public void NullContentDoesNotCrash()
		{
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				refreshVisualizer.Content = null;
				refreshVisualizer.RequestRefresh();
			});
		}

		[TestMethod]
		public void NullInfoProviderDoesNotCrash()
		{
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				Compositor compositor = ElementCompositionPreview.GetElementVisual(refreshVisualizer).Compositor;
				RefreshInfoProviderImpl refreshInfoProviderImpl = new RefreshInfoProviderImpl(compositor);
				((IRefreshVisualizerPrivate)refreshVisualizer).InfoProvider = refreshInfoProviderImpl;
				refreshVisualizer.RequestRefresh();
				((IRefreshVisualizerPrivate)refreshVisualizer).InfoProvider = null;
				refreshVisualizer.RequestRefresh();
			});
		}

		[TestMethod]
		public void RefreshRequestedEventFires()
		{
			int refreshRequestedCount = 0;

			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				refreshVisualizer.RefreshRequested += (RefreshVisualizer sender, RefreshRequestedEventArgs args) => { refreshRequestedCount++; };
				refreshVisualizer.RequestRefresh();
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual<int>(1, refreshRequestedCount);
			});
		}

		[TestMethod]
		public void RefreshCompletedDeferralCompletedOffUIThread()
		{
			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				Compositor compositor = ElementCompositionPreview.GetElementVisual(refreshVisualizer).Compositor;
				RefreshInfoProviderImpl refreshInfoProviderImpl = new RefreshInfoProviderImpl(compositor);
				((IRefreshVisualizerPrivate)refreshVisualizer).InfoProvider = refreshInfoProviderImpl;
				refreshVisualizer.RefreshRequested += (RefreshVisualizer sender, RefreshRequestedEventArgs args) =>
				{
					Deferral def = args.GetDeferral();

					Windows.System.Threading.ThreadPool.RunAsync(
						(IAsyncAction action) =>
						{
							Log.Comment("RefreshVisualizer.RefreshRequested event handler");
							def.Complete();
						}).AsTask().Wait();
				};

				Verify.AreEqual<int>(0, refreshInfoProviderImpl.OnRefreshStartedCalls);
				Verify.AreEqual<int>(0, refreshInfoProviderImpl.OnRefreshCompletedCalls);

				Content = refreshVisualizer;

				refreshVisualizer.RequestRefresh();
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = (RefreshVisualizer)Content;
				RefreshInfoProviderImpl refreshInfoProviderImpl = ((RefreshInfoProviderImpl)((IRefreshVisualizerPrivate)refreshVisualizer).InfoProvider);

				Verify.AreEqual<int>(1, refreshInfoProviderImpl.OnRefreshStartedCalls);
				//Since we are completing the deferral off the UI thread the refresh never completed.
				Verify.AreEqual<int>(0, refreshInfoProviderImpl.OnRefreshCompletedCalls);
			});

		}

		[TestMethod]
		public void RefreshStateChangedEventFires()
		{
			int refreshStateChangedCount = 0;

			RunOnUIThread.Execute(() =>
			{
				RefreshVisualizer refreshVisualizer = new RefreshVisualizer();
				refreshVisualizer.RefreshStateChanged += (RefreshVisualizer sender, RefreshStateChangedEventArgs args) => { refreshStateChangedCount++; };
				refreshVisualizer.RequestRefresh();
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual<int>(2, refreshStateChangedCount);
			});
		}
	}

	class RefreshInfoProviderImpl : IRefreshInfoProvider
	{
		private double executionRatio;
		private CompositionPropertySet compositionProperties;
		private bool isInteractingForRefresh;

		public int OnRefreshCompletedCalls;
		public int OnRefreshStartedCalls;

		public RefreshInfoProviderImpl(Compositor compositor)
		{
			executionRatio = 0.8f;
			compositionProperties = compositor.CreatePropertySet();
			compositionProperties.InsertScalar(InteractionRatioCompositionProperty, 0.0f);
			isInteractingForRefresh = false;
			OnRefreshCompletedCalls = 0;
			OnRefreshStartedCalls = 0;
		}

		public void SetCustomInteractionRatio(float value)
		{
			compositionProperties.InsertScalar(InteractionRatioCompositionProperty, value);
		}

		public void SetCustomIsInteractingForRefresh(bool value)
		{
			isInteractingForRefresh = value;
		}

		public void RaiseInteractionRatioChangedEvent(float interactionRatio)
		{
			if (this.InteractionRatioChanged != null)
			{
				this.InteractionRatioChanged.Invoke(this, PullToRefreshHelperTestApi.CreateRefreshInteractionRatioChangedEventArgsInstance(interactionRatio));
			}
		}

		public void RaiseIsInteractionForRefreshChangedEvent()
		{
			if (this.IsInteractingForRefreshChanged != null)
			{
				this.IsInteractingForRefreshChanged.Invoke(this, new EventArgs());
			}
		}

		public double ExecutionRatio
		{
			get
			{
				return executionRatio;
			}

			set
			{
				executionRatio = value;
			}
		}

		public CompositionPropertySet CompositionProperties
		{
			get
			{
				return compositionProperties;
			}
		}

		public string InteractionRatioCompositionProperty
		{
			get
			{
				return "InteractionRatio";
			}
		}

		public bool IsInteractingForRefresh
		{
			get
			{
				return isInteractingForRefresh;
			}
		}

		public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

		public void OnRefreshCompleted()
		{
			OnRefreshCompletedCalls++;

			if (this.RefreshCompleted != null)
			{
				RefreshCompleted.Invoke(this, new EventArgs());
			}
		}

		public void OnRefreshStarted()
		{
			OnRefreshStartedCalls++;
			if (this.RefreshStarted != null)
			{
				RefreshStarted.Invoke(this, new EventArgs());
			}
		}
	}
}
