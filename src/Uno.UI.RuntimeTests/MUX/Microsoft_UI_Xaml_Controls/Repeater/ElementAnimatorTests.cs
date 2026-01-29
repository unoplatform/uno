// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0618 // Animator is obsolete, use ItemTransitionProvider instead

using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using MUXControlsTestApp.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using RecyclingElementFactory = Microsoft.UI.Xaml.Controls.RecyclingElementFactory;
using RecyclePool = Microsoft.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using ItemsRepeaterScrollHost = Microsoft.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using AnimationContext = Microsoft.UI.Xaml.Controls.AnimationContext;
using Private.Infrastructure;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class ElementAnimatorTests : MUXApiTestBase
	{
		[TestMethod]
		[Ignore("UNO: ManualResetEvent not supported on WASM for now")]
		public async Task ValidateElementAnimator()
		{
			ItemsRepeater repeater = null;
			ElementAnimatorDerived animator = null;
			var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)));
			var renderingEvent = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var elementFactory = new RecyclingElementFactory();
				elementFactory.RecyclePool = new RecyclePool();
				elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> 
						  <TextBlock Text='{Binding}' Height='50' />
					  </DataTemplate>");

				CompositionTarget.Rendering += (sender, args) =>
				{
					renderingEvent.Set();
				};

				repeater = new ItemsRepeater()
				{
					ItemsSource = data,
					ItemTemplate = elementFactory,
				};

				Content = new ItemsRepeaterScrollHost()
				{
					Width = 400,
					Height = 800,
					ScrollViewer = new ScrollViewer
					{
						Content = repeater
					}
				};
			});

			await TestServices.WindowHelper.WaitForIdle();
			Verify.IsTrue(await renderingEvent.WaitOne(), "Waiting for rendering event");

			List<CallInfo> showCalls = new List<CallInfo>();
			List<CallInfo> hideCalls = new List<CallInfo>();
			List<CallInfo> boundsChangeCalls = new List<CallInfo>();
			RunOnUIThread.Execute(() =>
			{
				animator = new ElementAnimatorDerived()
				{
					HasShowAnimationValue = true,
					HasHideAnimationValue = true,
					HasBoundsChangeAnimationValue = true,
					StartShowAnimationFunc = (UIElement element, AnimationContext context) =>
					{
						showCalls.Add(new CallInfo(repeater.GetElementIndex(element), context));
					},

					StartHideAnimationFunc = (UIElement element, AnimationContext context) =>
					{
						hideCalls.Add(new CallInfo(repeater.GetElementIndex(element), context));
					},

					StartBoundsChangeAnimationFunc = (UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds) =>
					{
						boundsChangeCalls.Add(new CallInfo(repeater.GetElementIndex(element), context, oldBounds, newBounds));
					}
				};
				repeater.Animator = animator;

				renderingEvent.Reset();
				data.Insert(0, "new item");
				data.RemoveAt(2);
			});

			Verify.IsTrue(await renderingEvent.WaitOne(), "Waiting for rendering event");
			await TestServices.WindowHelper.WaitForIdle();

			Verify.AreEqual(1, showCalls.Count);
			var call = showCalls[0];
			Verify.AreEqual(0, call.Index);
			Verify.AreEqual(AnimationContext.CollectionChangeAdd, call.Context);

			Verify.AreEqual(1, hideCalls.Count);
			call = hideCalls[0];
			Verify.AreEqual(-1, call.Index); // not in the repeater anymore
			Verify.AreEqual(AnimationContext.CollectionChangeRemove, call.Context);

			Verify.AreEqual(1, boundsChangeCalls.Count);
			call = boundsChangeCalls[0];
			Verify.AreEqual(1, call.Index);
			Verify.AreEqual(AnimationContext.CollectionChangeAdd | AnimationContext.CollectionChangeRemove, call.Context);
			Verify.AreEqual(0, call.OldBounds.Y);
			Verify.AreEqual(50, call.NewBounds.Y);

			showCalls.Clear();
			hideCalls.Clear();
			boundsChangeCalls.Clear();

			// Hookup just for show animations and validate.
			RunOnUIThread.Execute(() =>
			{
				animator.HasShowAnimationValue = true;
				animator.HasHideAnimationValue = false;
				animator.HasBoundsChangeAnimationValue = false;

				renderingEvent.Reset();
				data.Insert(0, "new item");
				data.RemoveAt(2);
			});

			Verify.IsTrue(await renderingEvent.WaitOne(), "Waiting for rendering event");
			await TestServices.WindowHelper.WaitForIdle();

			Verify.AreEqual(1, showCalls.Count);
			call = showCalls[0];
			Verify.AreEqual(0, call.Index);
			Verify.AreEqual(AnimationContext.CollectionChangeAdd, call.Context);

			Verify.AreEqual(0, hideCalls.Count);
			Verify.AreEqual(0, boundsChangeCalls.Count);
		}

		struct CallInfo
		{
			public CallInfo(int index, AnimationContext context)
			{
				Index = index;
				Context = context;

				OldBounds = default;
				NewBounds = default;
			}

			public CallInfo(int index, AnimationContext context, Rect oldBounds, Rect newBounds)
			{
				Index = index;
				Context = context;
				OldBounds = oldBounds;
				NewBounds = newBounds;
			}

			public int Index { get; set; }
			public AnimationContext Context { get; set; }
			public Rect OldBounds { get; set; }
			public Rect NewBounds { get; set; }

			public override string ToString()
			{
				return "Index:" + Index + " Context:" + Context + " OldBounds:" + OldBounds + " NewBounds" + NewBounds;
			}
		};
	}
}
