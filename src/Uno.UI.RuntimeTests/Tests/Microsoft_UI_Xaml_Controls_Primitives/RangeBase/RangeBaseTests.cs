// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference RangeBaseTests.h

using System.Threading.Tasks;
using Uno.UI.RuntimeTests;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Generic;

[TestClass]
[RequiresFullWindow]
public class RangeBaseTests
{
	[TestMethod]
	public async Task DoesFireRangeValueChangedEvent()
	{
		bool valueChangedEvent = false;
		Slider rangeBase = null;

		await RunOnUIThread(() =>
		{
			rangeBase = new Slider();
			rangeBase.Value = 0;

			rangeBase.ValueChanged += (s, e) => valueChangedEvent = true;

			WindowHelper.WindowContent = rangeBase;
		});

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// change the value to see if the event launches
			rangeBase.Value = 1;
		});

		await WindowHelper.WaitFor(() => valueChangedEvent);
	}

	[TestMethod]
	public async Task IsRangeValueKeptBetweenMaxAndMin()
	{
		bool valueChangedEvent = false;
		Slider rangeBase = null;

		await RunOnUIThread(() =>
		{
			rangeBase = new Slider();
			rangeBase.Value = 0;
			rangeBase.Maximum = 100;
			rangeBase.Minimum = 0;

			// each time the value is changed, we'll check if the value is between max and min.
			rangeBase.ValueChanged += (s, e) =>
			{
				VERIFY_IS_TRUE(rangeBase.Minimum <= rangeBase.Maximum
					&& rangeBase.Value >= rangeBase.Minimum
					&& rangeBase.Value <= rangeBase.Maximum);
				valueChangedEvent = true;
			};

			WindowHelper.WindowContent = rangeBase;
		});

		await WindowHelper.WaitForIdle();

		// Testing for edge conditions to see if value is kept between Maximum and Minimum
		// test case 1: Regular case.
		await RunOnUIThread(() =>
		{
			rangeBase.Minimum = 0;
			rangeBase.Maximum = 100;
			rangeBase.Value = 50;
		});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 2: Value smaller than minimum
		await RunOnUIThread(() =>

			{
				rangeBase.Minimum = 0;
				rangeBase.Maximum = 100;
				rangeBase.Value = -50;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 3: Value larger than maximum
		await RunOnUIThread(() =>

			{
				rangeBase.Minimum = 100;
				rangeBase.Maximum = 0;
				rangeBase.Value = 150;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 4: Maximum smaller than minimum
		await RunOnUIThread(() =>

			{
				rangeBase.Maximum = 0;
				rangeBase.Minimum = 50;
				rangeBase.Value = -50;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 5: Negative minimum, positive maximum
		await RunOnUIThread(() =>

			{
				rangeBase.Minimum = -100;
				rangeBase.Maximum = 100;
				rangeBase.Value = 50;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 6: Negative minimum, negative maximum, value equals minimum.
		await RunOnUIThread(() =>

			{
				rangeBase.Minimum = -100;
				rangeBase.Maximum = -50;
				rangeBase.Value = -100;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;

		// test case 7: Negative minimum, positive maximum, value equals maximum.
		await RunOnUIThread(() =>

			{
				rangeBase.Minimum = -0.5;
				rangeBase.Maximum = 0.5;
				rangeBase.Value = 0.5;
			});

		await WindowHelper.WaitFor(() => valueChangedEvent);
		valueChangedEvent = false;
	}

	[TestMethod]
	public async Task MinMaxValueSetThroughMarkupWork()
	{
		const string typeName = "Slider";
		await RunOnUIThread(() =>

				{
					var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Minimum='10' Value='15' Maximum='20'/>",
						typeName));

					VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
					VERIFY_ARE_EQUAL(15, rangeBase.Value);
					VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
				});

		await RunOnUIThread(() =>

		{
			var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Minimum='10' Maximum='20' Value='15'/>",
						typeName));

			VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
			VERIFY_ARE_EQUAL(15, rangeBase.Value);
			VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
		});

		await RunOnUIThread(() =>

		{
			var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Value='15' Minimum='10' Maximum='20'/>",
						typeName));

			VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
			VERIFY_ARE_EQUAL(15, rangeBase.Value);
			VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
		});

		await RunOnUIThread(() =>

		{
			var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Value='15' Maximum='20' Minimum='10'/>",
						typeName));

			VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
			VERIFY_ARE_EQUAL(15, rangeBase.Value);
			VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
		});

		await RunOnUIThread(() =>
		{
			var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Maximum='20' Value='15' Minimum='10'/>",
						typeName));

			VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
			VERIFY_ARE_EQUAL(15, rangeBase.Value);
			VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
		});

		await RunOnUIThread(() =>

		{
			var rangeBase = (RangeBase)XamlReader.Load(string.Format(
						"<{0} xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Maximum='20' Minimum='10' Value='15'/>",
						typeName));

			VERIFY_ARE_EQUAL(10, rangeBase.Minimum);
			VERIFY_ARE_EQUAL(15, rangeBase.Value);
			VERIFY_ARE_EQUAL(20, rangeBase.Maximum);
		});
	}
}
