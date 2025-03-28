// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

partial class ScrollPresenterTests : MUXApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Create a bunch of snap points with invalid arguments.")]
	public void SnapPointsWithInvalidArgsThrow()
	{
		RunOnUIThread.Execute(() =>
		{
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset: 10, interval: 0, start: 10, end: 100, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset: 10, interval: -1, start: 10, end: 100, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 1, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 10, alignment: ScrollSnapPointsAlignment.Near); });
#if ApplicableRangeType
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset:   1, interval: 10, start:   1, end:  10, applicableRange: -10, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset:   1, interval: 10, start:   1, end:  10, applicableRange:   0, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset:  50, interval: 10, start: 100, end: 200, applicableRange:   2, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new RepeatedScrollSnapPoint(offset: 250, interval: 10, start: 100, end: 200, applicableRange:   2, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new ScrollSnapPoint(snapPointValue: 0, applicableRange:  0, alignment: ScrollSnapPointsAlignment.Near); });
			Verify.Throws<ArgumentException>(() => { new ScrollSnapPoint(snapPointValue: 0, applicableRange: -1, alignment: ScrollSnapPointsAlignment.Near); });
#endif
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verify that overlapping repeated snap points throw while adjacent ones do not.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public void OverlappingRepeatedSnapPointsThrow()
	{
		RunOnUIThread.Execute(() =>
		{
			ScrollPresenter scrollPresenter = new ScrollPresenter();
			RepeatedScrollSnapPoint snapPoint1 = new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 100, alignment: ScrollSnapPointsAlignment.Near);
			RepeatedScrollSnapPoint snapPoint2 = new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 100, alignment: ScrollSnapPointsAlignment.Near);
			RepeatedScrollSnapPoint snapPoint3 = new RepeatedScrollSnapPoint(offset: 0, interval: 2, start: 0, end: 12, alignment: ScrollSnapPointsAlignment.Near);
			RepeatedScrollSnapPoint snapPoint4 = new RepeatedScrollSnapPoint(offset: 0, interval: 2, start: 0, end: 10, alignment: ScrollSnapPointsAlignment.Near);
			RepeatedScrollSnapPoint snapPoint5 = new RepeatedScrollSnapPoint(offset: 100, interval: 2, start: 100, end: 200, alignment: ScrollSnapPointsAlignment.Near);

			scrollPresenter.VerticalSnapPoints.Add(snapPoint1);
			Verify.Throws<ArgumentException>(() => { scrollPresenter.VerticalSnapPoints.Add(snapPoint2); });
			Verify.Throws<ArgumentException>(() => { scrollPresenter.VerticalSnapPoints.Add(snapPoint3); });
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint4);
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint5);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Add and remove snap points and make sure the corresponding collections look correct.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public async Task CanAddAndRemoveSnapPointsFromAScrollPresenter()
	{
		await CanAddAndRemoveSnapPointsFromAScrollPresenter(ScrollSnapPointsAlignment.Near);
		await CanAddAndRemoveSnapPointsFromAScrollPresenter(ScrollSnapPointsAlignment.Center);
		await CanAddAndRemoveSnapPointsFromAScrollPresenter(ScrollSnapPointsAlignment.Far);
	}

	private async Task CanAddAndRemoveSnapPointsFromAScrollPresenter(ScrollSnapPointsAlignment alignment)
	{
		ScrollPresenter scrollPresenter = null;
		ScrollSnapPoint snapPoint2 = null;
		RepeatedScrollSnapPoint snapPoint3 = null;
		RepeatedScrollSnapPoint snapPoint4 = null;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter = new ScrollPresenter();
			ScrollSnapPoint snapPoint1 = new ScrollSnapPoint(snapPointValue: 10, alignment: alignment);
			snapPoint2 = new ScrollSnapPoint(snapPointValue: 10, alignment: alignment);
			snapPoint3 = new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 100, alignment: alignment);
			snapPoint4 = new RepeatedScrollSnapPoint(offset: 100, interval: 10, start: 100, end: 200, alignment: alignment);
			ZoomSnapPoint snapPoint5 = new ZoomSnapPoint(snapPointValue: 10);
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint1);
			scrollPresenter.VerticalSnapPoints.Add(snapPoint1);
			scrollPresenter.ZoomSnapPoints.Add(snapPoint5);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(1, scrollPresenter.HorizontalSnapPoints.Count);
			Verify.AreEqual<int>(1, scrollPresenter.VerticalSnapPoints.Count);
			Verify.AreEqual<int>(1, scrollPresenter.ZoomSnapPoints.Count);
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint2);
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint3);
			scrollPresenter.HorizontalSnapPoints.Add(snapPoint4);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(4, scrollPresenter.HorizontalSnapPoints.Count);
			scrollPresenter.HorizontalSnapPoints.Remove(snapPoint2);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(3, scrollPresenter.HorizontalSnapPoints.Count);
			scrollPresenter.HorizontalSnapPoints.Remove(snapPoint2);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(3, scrollPresenter.HorizontalSnapPoints.Count);
			scrollPresenter.HorizontalSnapPoints.Clear();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(0, scrollPresenter.HorizontalSnapPoints.Count);
			Verify.AreEqual<int>(1, scrollPresenter.VerticalSnapPoints.Count);
			Verify.AreEqual<int>(1, scrollPresenter.ZoomSnapPoints.Count);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Add scroll snap points with various alignments.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public async Task CanAddScrollSnapPointsWithMixedAlignments()
	{
		ScrollPresenter scrollPresenter = null;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter = new ScrollPresenter();
			ScrollSnapPoint nearSnapPoint = new ScrollSnapPoint(snapPointValue: 10, alignment: ScrollSnapPointsAlignment.Near);
			ScrollSnapPoint centerSnapPoint = new ScrollSnapPoint(snapPointValue: 20, alignment: ScrollSnapPointsAlignment.Center);
			ScrollSnapPoint farSnapPoint = new ScrollSnapPoint(snapPointValue: 30, alignment: ScrollSnapPointsAlignment.Far);
			RepeatedScrollSnapPoint nearRepeatedScrollSnapPoint = new RepeatedScrollSnapPoint(offset: 50, interval: 10, start: 50, end: 100, alignment: ScrollSnapPointsAlignment.Near);
			RepeatedScrollSnapPoint centerRepeatedScrollSnapPoint = new RepeatedScrollSnapPoint(offset: 180, interval: 10, start: 175, end: 225, alignment: ScrollSnapPointsAlignment.Center);
			RepeatedScrollSnapPoint farRepeatedScrollSnapPoint = new RepeatedScrollSnapPoint(offset: 280, interval: 5, start: 280, end: 300, alignment: ScrollSnapPointsAlignment.Far);
			scrollPresenter.HorizontalSnapPoints.Add(nearSnapPoint);
			scrollPresenter.HorizontalSnapPoints.Add(centerSnapPoint);
			scrollPresenter.HorizontalSnapPoints.Add(farSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(nearSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(centerSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(farSnapPoint);
			scrollPresenter.HorizontalSnapPoints.Add(nearRepeatedScrollSnapPoint);
			scrollPresenter.HorizontalSnapPoints.Add(centerRepeatedScrollSnapPoint);
			scrollPresenter.HorizontalSnapPoints.Add(farRepeatedScrollSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(nearRepeatedScrollSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(centerRepeatedScrollSnapPoint);
			scrollPresenter.VerticalSnapPoints.Add(farRepeatedScrollSnapPoint);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual<int>(6, scrollPresenter.HorizontalSnapPoints.Count);
			Verify.AreEqual<int>(6, scrollPresenter.VerticalSnapPoints.Count);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Add the same snap points to multiple collections and ensure they use collection-specific data.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public async Task CanShareSnapPointsInMultipleCollections()
	{
		ScrollPresenter scrollPresenter1 = null;
		ScrollPresenter scrollPresenter2 = null;
		ScrollPresenter scrollPresenter3 = null;

		ScrollSnapPoint scrollSnapPoint1 = null;
		ScrollSnapPoint scrollSnapPoint2 = null;
		ScrollSnapPoint scrollSnapPoint3 = null;

		RepeatedScrollSnapPoint repeatedScrollSnapPoint1 = null;
		RepeatedScrollSnapPoint repeatedScrollSnapPoint2 = null;
		RepeatedScrollSnapPoint repeatedScrollSnapPoint3 = null;

		ZoomSnapPoint zoomSnapPoint1 = null;
		ZoomSnapPoint zoomSnapPoint2 = null;
		ZoomSnapPoint zoomSnapPoint3 = null;

		RunOnUIThread.Execute(() =>
		{
			scrollPresenter1 = new ScrollPresenter();
			scrollPresenter2 = new ScrollPresenter();
			scrollPresenter3 = new ScrollPresenter();

			scrollSnapPoint1 = new ScrollSnapPoint(snapPointValue: 10, alignment: ScrollSnapPointsAlignment.Near);
			scrollSnapPoint2 = new ScrollSnapPoint(snapPointValue: 20, alignment: ScrollSnapPointsAlignment.Near);
			scrollSnapPoint3 = new ScrollSnapPoint(snapPointValue: 30, alignment: ScrollSnapPointsAlignment.Near);

			repeatedScrollSnapPoint1 = new RepeatedScrollSnapPoint(offset: 10, interval: 10, start: 10, end: 100, alignment: ScrollSnapPointsAlignment.Near);
			repeatedScrollSnapPoint2 = new RepeatedScrollSnapPoint(offset: 200, interval: 10, start: 110, end: 200, alignment: ScrollSnapPointsAlignment.Near);
			repeatedScrollSnapPoint3 = new RepeatedScrollSnapPoint(offset: 300, interval: 10, start: 210, end: 300, alignment: ScrollSnapPointsAlignment.Near);

			zoomSnapPoint1 = new ZoomSnapPoint(snapPointValue: 1);
			zoomSnapPoint2 = new ZoomSnapPoint(snapPointValue: 2);
			zoomSnapPoint3 = new ZoomSnapPoint(snapPointValue: 3);

			scrollPresenter1.HorizontalSnapPoints.Add(scrollSnapPoint1);
			scrollPresenter1.HorizontalSnapPoints.Add(scrollSnapPoint2);
			scrollPresenter1.VerticalSnapPoints.Add(scrollSnapPoint1);
			scrollPresenter1.VerticalSnapPoints.Add(scrollSnapPoint3);

			scrollPresenter2.HorizontalSnapPoints.Add(repeatedScrollSnapPoint1);
			scrollPresenter2.HorizontalSnapPoints.Add(repeatedScrollSnapPoint2);
			scrollPresenter2.VerticalSnapPoints.Add(repeatedScrollSnapPoint1);
			scrollPresenter2.VerticalSnapPoints.Add(repeatedScrollSnapPoint3);

			scrollPresenter1.ZoomSnapPoints.Add(zoomSnapPoint1);
			scrollPresenter1.ZoomSnapPoints.Add(zoomSnapPoint2);
			scrollPresenter2.ZoomSnapPoints.Add(zoomSnapPoint1);
			scrollPresenter2.ZoomSnapPoints.Add(zoomSnapPoint3);

			scrollPresenter3.HorizontalSnapPoints.Add(scrollSnapPoint1);
			scrollPresenter3.HorizontalSnapPoints.Add(scrollSnapPoint1);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Vector2 horizontalScrollSnapPoint11ApplicableZone = ScrollPresenterTestHooks.GetHorizontalSnapPointActualApplicableZone(scrollPresenter1, scrollSnapPoint1);
			Vector2 verticalScrollSnapPoint11ApplicableZone = ScrollPresenterTestHooks.GetVerticalSnapPointActualApplicableZone(scrollPresenter1, scrollSnapPoint1);
			Log.Comment("horizontalScrollSnapPoint11ApplicableZone=" + horizontalScrollSnapPoint11ApplicableZone.ToString());
			Log.Comment("verticalScrollSnapPoint11ApplicableZone=" + verticalScrollSnapPoint11ApplicableZone.ToString());

			Vector2 horizontalScrollSnapPoint21ApplicableZone = ScrollPresenterTestHooks.GetHorizontalSnapPointActualApplicableZone(scrollPresenter2, repeatedScrollSnapPoint1);
			Vector2 verticalScrollSnapPoint21ApplicableZone = ScrollPresenterTestHooks.GetVerticalSnapPointActualApplicableZone(scrollPresenter2, repeatedScrollSnapPoint1);
			Log.Comment("horizontalScrollSnapPoint21ApplicableZone=" + horizontalScrollSnapPoint21ApplicableZone.ToString());
			Log.Comment("verticalScrollSnapPoint21ApplicableZone=" + verticalScrollSnapPoint21ApplicableZone.ToString());

			Vector2 zoomSnapPoint11ApplicableZone = ScrollPresenterTestHooks.GetZoomSnapPointActualApplicableZone(scrollPresenter1, zoomSnapPoint1);
			Vector2 zoomSnapPoint21ApplicableZone = ScrollPresenterTestHooks.GetZoomSnapPointActualApplicableZone(scrollPresenter2, zoomSnapPoint1);
			Log.Comment("zoomSnapPoint11ApplicableZone=" + zoomSnapPoint11ApplicableZone.ToString());
			Log.Comment("zoomSnapPoint21ApplicableZone=" + zoomSnapPoint21ApplicableZone.ToString());

			int combinationCount11 = ScrollPresenterTestHooks.GetHorizontalSnapPointCombinationCount(scrollPresenter1, scrollSnapPoint1);
			int combinationCount31 = ScrollPresenterTestHooks.GetHorizontalSnapPointCombinationCount(scrollPresenter3, scrollSnapPoint1);
			Log.Comment("combinationCount11=" + combinationCount11);
			Log.Comment("combinationCount31=" + combinationCount31);

			Log.Comment("Expecting different applicable zones for ScrollSnapPoint in horizontal and vertical collections");
			Verify.AreEqual<float>(15.0f, horizontalScrollSnapPoint11ApplicableZone.Y);
			Verify.AreEqual<float>(20.0f, verticalScrollSnapPoint11ApplicableZone.Y);

			Log.Comment("Expecting identical applicable zones for RepeatedScrollSnapPoint in horizontal and vertical collections");
			Verify.AreEqual<float>(10.0f, horizontalScrollSnapPoint21ApplicableZone.X);
			Verify.AreEqual<float>(10.0f, verticalScrollSnapPoint21ApplicableZone.X);
			Verify.AreEqual<float>(100.0f, horizontalScrollSnapPoint21ApplicableZone.Y);
			Verify.AreEqual<float>(100.0f, verticalScrollSnapPoint21ApplicableZone.Y);

			Log.Comment("Expecting different applicable zones for ZoomSnapPoint in two zoom collections");
			Verify.AreEqual<float>(1.5f, zoomSnapPoint11ApplicableZone.Y);
			Verify.AreEqual<float>(2.0f, zoomSnapPoint21ApplicableZone.Y);

			Log.Comment("Expecting different combination counts for ScrollSnapPoint in two horizontal collections");
			Verify.AreEqual<int>(0, combinationCount11);
			Verify.AreEqual<int>(1, combinationCount31);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Snap to the first instance of a repeated scroll snap point and ensure it is placed after the Start value.")]
	[Ignore("Fails for missing InteractionTracker features")]
	public async Task SnapToFirstRepeatedScrollSnapPoint()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Jump to absolute offsets
		await ScrollTo(scrollPresenter, 60.0, 0.0, ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Default);

		// Add scroll repeated snap point with different offset and start.
		RunOnUIThread.Execute(() =>
		{
			RepeatedScrollSnapPoint snapPoint = new RepeatedScrollSnapPoint(
				offset: 50,
				interval: 60,
				start: 10,
				end: 1190,
				alignment: ScrollSnapPointsAlignment.Near);

			scrollPresenter.HorizontalSnapPoints.Add(snapPoint);
		});

		// Flick with horizontal offset velocity to naturally land around offset 15.
		await AddScrollVelocity(scrollPresenter, horizontalVelocity: -165.0f, verticalVelocity: 0.0f, horizontalInertiaDecayRate: null, verticalInertiaDecayRate: null, hookViewChanged: false);

		RunOnUIThread.Execute(() =>
		{
			// HorizontalOffset expected to have snapped to first instance of repeated snap point: 50.
			Verify.AreEqual(50.0, scrollPresenter.HorizontalOffset);
			Verify.AreEqual(0.0, scrollPresenter.VerticalOffset);
			Verify.AreEqual(1.0f, scrollPresenter.ZoomFactor);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Snap to the first instance of a repeated zoom snap point and ensure it is placed after the Start value.")]
	[Ignore("Zoom is not yet supported in Uno.")]
	public async Task SnapToFirstRepeatedZoomSnapPoint()
	{
		ScrollPresenter scrollPresenter = null;
		Rectangle rectangleScrollPresenterContent = null;
		UnoAutoResetEvent scrollPresenterLoadedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			rectangleScrollPresenterContent = new Rectangle();
			scrollPresenter = new ScrollPresenter();

			SetupDefaultUI(scrollPresenter, rectangleScrollPresenterContent, scrollPresenterLoadedEvent);
		});

		await WaitForEvent("Waiting for Loaded event", scrollPresenterLoadedEvent);

		// Jump to absolute zoom factor, and center the content in the viewport.
		await ZoomTo(scrollPresenter,
			zoomFactor: 6.0f,
			centerPointX: 690.0f,
			centerPointY: 340.0f,
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Default);

		// Add zoom repeated snap point with different offset and start.
		RunOnUIThread.Execute(() =>
		{
			RepeatedZoomSnapPoint snapPoint = new RepeatedZoomSnapPoint(
				offset: 5,
				interval: 6,
				start: 1,
				end: 9);

			scrollPresenter.ZoomSnapPoints.Add(snapPoint);
		});

		// Flick with zoom factor velocity to naturally land around factor 1.5.
		await AddZoomVelocity(scrollPresenter,
			zoomFactorVelocity: -5.0f,
			inertiaDecayRate: 0.6675f,
			centerPointX: 150.0f,
			centerPointY: 100.0f,
			hookViewChanged: false);

		RunOnUIThread.Execute(() =>
		{
			// ZoomFactor expected to have snapped to first instance of repeated snap point: 5.
			// Scroll offsets do not snap and end close to 2850, 1400 for a centered content.
			Verify.IsTrue(Math.Abs(scrollPresenter.HorizontalOffset - 2850.0) < 1.0);
			Verify.IsTrue(Math.Abs(scrollPresenter.VerticalOffset - 1400.0) < 1.0);
			Verify.AreEqual(5.0f, scrollPresenter.ZoomFactor);
		});
	}
}
