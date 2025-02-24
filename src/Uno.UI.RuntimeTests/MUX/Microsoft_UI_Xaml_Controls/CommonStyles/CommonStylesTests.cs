// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using MUXControlsTestApp.Utilities;
using System.Linq;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using PlatformConfiguration = Common.PlatformConfiguration;
using OSVersion = Common.OSVersion;
using System.Collections.Generic;
using XamlControlsResources = Microsoft/* UWP don't rename */.UI.Xaml.Controls.XamlControlsResources;
using Windows.UI.Xaml.Markup;
using System;
using System.Text;
#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class CommonStylesApiTests : MUXApiTestBase
	{
		// TODO: Many tests from MUX CommonStylesApiTests.cpp are missing here and should be added in the future to Uno Platform.

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public void CornerRadiusFilterConverterTest()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone4))
			{
				Log.Comment("Corner radius is only available on RS5+");
				return;
			}

			RunOnUIThread.Execute(() =>
			{
				var root = (StackPanel)XamlReader.Load(
					@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                             xmlns:primitives='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls.Primitives'> 
                            <StackPanel.Resources>
                                <primitives:CornerRadiusFilterConverter x:Key='TopCornerRadiusFilterConverter' Filter='Top' Scale='2'/>
                                <primitives:CornerRadiusFilterConverter x:Key='RightCornerRadiusFilterConverter' Filter='Right'/>
                                <primitives:CornerRadiusFilterConverter x:Key='BottomCornerRadiusFilterConverter' Filter='Bottom'/>
                                <primitives:CornerRadiusFilterConverter x:Key='LeftCornerRadiusFilterConverter' Filter='Left'/>
                            </StackPanel.Resources>
							<Grid x:Name='SourceGrid' CornerRadius='6,6,6,6' />
                            <Grid x:Name='TopRadiusGrid'
                                CornerRadius='{Binding ElementName=SourceGrid, Path=CornerRadius, Converter={StaticResource TopCornerRadiusFilterConverter}}'>
                            </Grid>
                            <Grid x:Name='RightRadiusGrid'
                                CornerRadius='{Binding ElementName=SourceGrid, Path=CornerRadius, Converter={StaticResource RightCornerRadiusFilterConverter}}'>
                            </Grid>
                            <Grid x:Name='BottomRadiusGrid'
                                CornerRadius='{Binding ElementName=SourceGrid, Path=CornerRadius, Converter={StaticResource BottomCornerRadiusFilterConverter}}'>
                            </Grid>
                            <Grid x:Name='LeftRadiusGrid'
                                CornerRadius='{Binding ElementName=SourceGrid, Path=CornerRadius, Converter={StaticResource LeftCornerRadiusFilterConverter}}'>
                            </Grid>
                       </StackPanel>");

				var topRadiusGrid = (Grid)root.FindName("TopRadiusGrid");
				var rightRadiusGrid = (Grid)root.FindName("RightRadiusGrid");
				var bottomRadiusGrid = (Grid)root.FindName("BottomRadiusGrid");
				var leftRadiusGrid = (Grid)root.FindName("LeftRadiusGrid");

				Verify.AreEqual(new CornerRadius(12, 12, 0, 0), topRadiusGrid.CornerRadius);
				Verify.AreEqual(new CornerRadius(0, 6, 6, 0), rightRadiusGrid.CornerRadius);
				Verify.AreEqual(new CornerRadius(0, 0, 6, 6), bottomRadiusGrid.CornerRadius);
				Verify.AreEqual(new CornerRadius(6, 0, 0, 6), leftRadiusGrid.CornerRadius);
			});
		}
	}
}
