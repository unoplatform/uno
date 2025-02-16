using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Private.Infrastructure;

namespace UITests.Shared.Windows_Services_Store;

[Sample("Windows.Services.Store", "StoreContext", Description = "This page provides tests for StoreContext APIs as In App Reviews. When the StoreContext.TestMode is set to true, it should open the Fake Review Manager from Google Play.", ViewModelType = typeof(StoreContextTestsViewModel), IgnoreInSnapshotTests = true)]
public sealed partial class StoreContextTests : UserControl
{
	public StoreContextTests()
	{
		this.InitializeComponent();
	}
}

internal class StoreContextTestsViewModel(UnitTestDispatcherCompat dispatcher) : ViewModelBase(dispatcher)
{
	public Command ReviewCommand => new(async (p) =>
	{
		await Windows.Services.Store.StoreContext.GetDefault().RequestRateAndReviewAppAsync();
	});
}
