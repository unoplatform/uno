#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.Extensions;
using static Private.Infrastructure.TestServices.WindowHelper;
using static Windows.Foundation.Rect;
using EffectiveViewportChangedEventArgs = Microsoft.UI.Xaml.EffectiveViewportChangedEventArgs;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public partial class Given_FrameworkElement_EffectiveViewport
	{
#if __ANDROID__
		private Rect WindowBounds
		{
			get
			{
				var slot = LayoutInformation.GetLayoutSlot(Window.Current!.Content);
				var bounds = new Rect(0, 0, slot.Width, slot.Height);

				return bounds;
			}
		}
#else
		private Rect WindowBounds =>
#if HAS_UNO
			TestServices.WindowHelper.EmbeddedTestRoot.control?.XamlRoot?.Bounds ??
#endif
			TestServices.WindowHelper.XamlRoot.Bounds;
#endif

		private Point RootLocation
		{
			get
			{
				var root = (FrameworkElement)WindowContent;
				var windowBounds = WindowBounds;

				var x = X(root, windowBounds.Width);
				var y = Y(root, windowBounds.Height);
#if HAS_UNO // LayoutRound isn't public in UWP/WinUI. Ignore that block of code there for now.
				if (root.UseLayoutRounding)
				{
					x = root.LayoutRound(x);
					y = root.LayoutRound(y);
				}
#endif

				return new Point(x, y);
			}
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task EffectiveViewport_When_BottomRightAligned()
		{
			var sut = new Border { Width = 42, Height = 42, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
			var parent = new ScrollViewer { Width = 142, Height = 142, Content = new Grid { Children = { sut } } };

			var result = Empty;
			sut.EffectiveViewportChanged += (snd, e) => result = e.EffectiveViewport;

			WindowContent = parent;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				Assert.AreEqual(new Rect(-100, -100, 142, 142), result);
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_BasicVisualTree()
		{
			FrameworkElement sut;
			var tree = new Grid
			{
				//Clip = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) },
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Margin = new Thickness(42),
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Child = sut = new Border(),
					}
				}
			};

			var viewport = default(Rect);
			sut.EffectiveViewportChanged += (snd, e) => viewport = e.EffectiveViewport;

			WindowContent = tree;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				var relative = viewport; // EVP is expressed in local (i.e. relative to sut) coordinate space
				var absolute = sut.TransformToVisual(null).TransformBounds(viewport);

				relative.Should().Be(new Rect(-42, -42, tree.ActualWidth, tree.ActualHeight), because: "relative");
				absolute.Should().Be(new Rect(0, 0, tree.ActualWidth, tree.ActualHeight), because: "absolute");
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_ScrollViewer()
		{
			FrameworkElement sut;
			ScrollViewer sv;
			var tree = new Grid
			{
				Children =
				{
					(sv = new ScrollViewer
					{
						Width = 100,
						Height = 100,
						Margin = new Thickness(42),
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Content = sut = new Border
						{
							Width = 100,
							Height = 500
						}
					})
				}
			};

			var viewport = default(Rect);
			sut.EffectiveViewportChanged += (snd, e) => viewport = e.EffectiveViewport;

			WindowContent = tree;
			await WaitForIdle();

			sv.ChangeView(horizontalOffset: default, verticalOffset: 150, zoomFactor: default, disableAnimation: true);
			await WaitForIdle();

			await RetryAssert(() =>
			{
				var relative = viewport; // EVP is expressed in local (i.e. relative to sut) coordinate space
				var absolute = sut.TransformToVisual(null).TransformBounds(viewport);

				relative.Should().Be(new Rect(0, 150, 100, 100), because: "relative");
				absolute.Should().Be(new Rect(42, 42, 100, 100), because: "absolute");
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_Unconstrained()
		{
			/*
			<local:HeadedContent Header="Unconstrained" Background="#FF0018">
				<Border EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
			</local:HeadedContent>
			*/

			var sut = new Border { Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x18)) };
			using var vp = VP(sut);

			WindowContent = sut;
			await WaitForIdle();

			vp.Effective.Should().Be(WindowBounds);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		#region DataRows
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, double.NaN, 100)]
#if !__SKIA__
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, double.NaN, double.NaN)]
#endif
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Center, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Bottom, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Top, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Bottom, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Top, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Center, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 100, double.NaN)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, double.NaN, 100)]
		[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, double.NaN, double.NaN)]
		#endregion
		public async Task EVP_When_Constrained(HorizontalAlignment hAlign, VerticalAlignment vAlign, double width, double height)
		{
			/*
			<local:HeadedContent Header="Constrained" Background="#FFA52C">
				<Border Width="100" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
			</local:HeadedContent>
			*/

			var sut = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xA5, 0x2c)),
				HorizontalAlignment = hAlign,
				VerticalAlignment = vAlign,
				Width = width,
				Height = height,
			};
			using var vp = VP(sut);

			WindowContent = sut;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-RootLocation.X, -RootLocation.Y, WindowBounds.Width, WindowBounds.Height));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if __ANDROID__
		[Ignore("Fails on emulator < API 30, like CI, due to invalid WindowBounds / VisibleBounds")]
#endif
		public async Task EVP_When_SVTree()
		{
			/*
			<local:HeadedContent Header="SV tree" Background="#FFFF41">
				<ScrollViewer Loaded="ShowEVPTree">
					<Border Height="2048" Width="200" Style="{StaticResource EVPContainer}" x:Name="TheBorder"/>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = false, canVerticallyScroll = true;

			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 512,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = sut = new Border
					{
						Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x41)),
						Width = 256,
						Height = 1024
					}
				}
			};
			using var vp = VPTree(root, sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp[sv].Effective.Should().Be(WindowBounds);
				vp.Of<ScrollContentPresenter>().Effective.Should().Be(WindowBounds);
#if __ANDROID__ || __IOS__ // same reason as Ignore of EVP_When_ConstrainedInNonScrollableSV
				vp[sut].Effective.Width.Should().Be(512);
				vp[sut].Effective.Height.Should().Be(512);
#else
				vp[sut].Effective.Should().Be(new Rect(-128, 0, 512, 512));
#endif
			});

			sv.ChangeView(null, verticalOffset: 512, null, disableAnimation: true);

			await RetryAssert(() =>
			{
				vp[sv].Effective.Should().Be(WindowBounds);
#if !__SKIA__ && !__WASM__ && !__ANDROID__ && !__IOS__
				vp.Of<ScrollContentPresenter>().Effective.Should().Be(WindowBounds);
#endif
#if __ANDROID__ || __IOS__ // same reason as Ignore of EVP_When_ConstrainedInNonScrollableSV
				vp[sut].Effective.Width.Should().Be(512);
				vp[sut].Effective.Height.Should().Be(512);
				vp[sut].Effective.Y.Should().Be(512);
#else
				vp[sut].Effective.Should().Be(new Rect(-128, 512, 512, 512));
#endif
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]

		public async Task EVP_When_UnconstrainedClipped()
		{
			/*
			<local:HeadedContent Header="Unconstrained clipped" Background="#008018">
				<Grid>
					<Grid.Clip>
						<RectangleGeometry Rect="50,50,100,100"></RectangleGeometry>
					</Grid.Clip>
					<Border EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
				</Grid>
			</local:HeadedContent>
			*/

			var sut = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x80, 0x18)),
				Clip = new RectangleGeometry { Rect = new Rect(50, 50, 100, 100) }
			};
			using var vp = VP(sut);

			WindowContent = sut;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(WindowBounds);
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_ConstrainedInSV()
		{
			/*
			<local:HeadedContent Header="Constrained SV" Background="#0000F9">
				<ScrollViewer Width="100" Height="100">
					<Border Width="200" Height="200" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = true, canVerticallyScroll = true;

			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 100,
					Height = 100,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = sut = new Border
					{
						Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xF9)),
						Width = 200,
						Height = 200
					}
				}
			};
			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(0, 0, 100, 100));
			});

			sv.ChangeView(sv.ScrollableWidth, sv.ScrollableHeight, null, disableAnimation: true);

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(100, 100, 100, 100));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		[DataRow(false, false)]
		[DataRow(true, false)]
		[DataRow(false, true)]
		[DataRow(true, true)]
#if __ANDROID__ || __IOS__
		[Ignore(
			"On Android and iOS the ScrollHost is not the (native)SCP but the SV, so alignments are not taken in consideration when computing the scrollport "
			+ "(which is used as viewport for children). We will get instead 100x100@0,0.")]
#endif
		public async Task EVP_When_ConstrainedInNonScrollableSV(bool canHorizontallyScroll, bool canVerticallyScroll)
		{
			/*
			<local:HeadedContent Header="Constrained non scrollable SV" Background="#86007D">
				<ScrollViewer Width="100" Height="100">
					<Border Width="90" Height="90" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 100,
					Height = 100,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = sut = new Border
					{
						Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x86, 0x00, 0x7D)),
						Width = 90,
						Height = 90
					}
				}
			};
			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-5, -5, 100, 100));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_NestedSV()
		{
			/*
			<local:HeadedContent Header="Nested SV" Background="#FF0018">
				<ScrollViewer x:Name="sv1" EffectiveViewportChanged="ShowEVP">
					<Border>
						<ScrollViewer Width="1024" Height="1024" Margin="50, 256" x:Name="sv2" EffectiveViewportChanged="ShowEVP">
							<Border Width="2048" Height="2048" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
						</ScrollViewer>
					</Border>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canSV1HorizontallyScroll = true, canSV1VerticallyScroll = true, canSV2HorizontallyScroll = true, canSV2VerticallyScroll = true;
			const int x = 1024, y = 1024;

			Border sut;
			ScrollViewer sv1, sv2;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Height = 512,
				Width = 512,
				Child = sv1 = new ScrollViewer
				{
					Background = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x00, 0x18)),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canSV1HorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canSV1VerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canSV1HorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canSV1VerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new Border // This border is required for https://github.com/unoplatform/uno/issues/7000
					{
						Child = sv2 = new ScrollViewer
						{
							Width = 1024,
							Height = 1024,
							Margin = new Thickness(x, y, x, y),
							HorizontalAlignment = HorizontalAlignment.Left,
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalScrollBarVisibility = canSV2HorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
							VerticalScrollBarVisibility = canSV2VerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
							HorizontalScrollMode = canSV2HorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
							VerticalScrollMode = canSV2VerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
							Content = sut = new Border
							{
								Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x18)),
								Width = 2048,
								Height = 2048
							}
						}
					}
				}
			};

			using var vp1 = VP(sv1);
			using var vp2 = VP(sv2);
			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			IEnumerable<(double sv1X, double sv1Y, double sv2X, double sv2Y, bool empty)> GetTestCases()
			{
				return ChecksSv2Edges().Aggregate(
					CheckSv1Edges(),
					(cases, @case) => cases.Concat(@case));

				IEnumerable<(double sv1X, double sv1Y, double sv2X, double sv2Y, bool empty)> CheckSv1Edges()
				{
					yield return (0, 0, 0, 0, true);
					yield return (0, y, 0, 0, true);
					yield return (x, 0, 0, 0, true);

					yield return (sv1.ScrollableWidth - 0, 0, 0, 0, true);
					yield return (sv1.ScrollableWidth - 0, y, 0, 0, true);
					yield return (sv1.ScrollableWidth - x, 0, 0, 0, true);

					yield return (0, sv1.ScrollableHeight - 0, 0, 0, true);
					yield return (0, sv1.ScrollableHeight - y, 0, 0, true);
					yield return (x, sv1.ScrollableHeight - 0, 0, 0, true);

					yield return (sv1.ScrollableWidth - 0, sv1.ScrollableHeight - 0, 0, 0, true);
					yield return (sv1.ScrollableWidth - 0, sv1.ScrollableHeight - y, 0, 0, true);
					yield return (sv1.ScrollableWidth - x, sv1.ScrollableHeight - 0, 0, 0, true);
				}

				IEnumerable<IEnumerable<(double sv1X, double sv1Y, double sv2X, double sv2Y, bool empty)>> ChecksSv2Edges()
				{
					yield return CheckSv2EdgesCase(x, y);
					yield return CheckSv2EdgesCase(x, sv1.ScrollableHeight - y);
					yield return CheckSv2EdgesCase(sv1.ScrollableWidth - x, y);
					yield return CheckSv2EdgesCase(sv1.ScrollableWidth - x, sv1.ScrollableHeight - y);
				}

				IEnumerable<(double sv1X, double sv1Y, double sv2X, double sv2Y, bool empty)> CheckSv2EdgesCase(double baseX, double baseY)
				{
					yield return (baseX, baseY, 0, 0, false);
					yield return (baseX, baseY, 0, 42, false);
					yield return (baseX, baseY, 42, 0, false);
					yield return (baseX, baseY, 42, 42, false);
					yield return (baseX, baseY - 42, 0, 0, false);
					yield return (baseX - 42, baseY, 0, 0, false);
					yield return (baseX - 42, baseY - 42, 0, 0, false);
				}
			}

			foreach (var testCase in GetTestCases())
			{
				var testName = $"sv1={testCase.sv1X:F0},{testCase.sv1Y:F2} | sv2={testCase.sv2X:F0},{testCase.sv2Y:F2} => ";

				sv1.ChangeView(testCase.sv1X, testCase.sv1Y, null, disableAnimation: true);
				sv2.ChangeView(testCase.sv2X, testCase.sv2Y, null, disableAnimation: true);
				await WaitForIdle();

				await RetryAssert(testName, () =>
				{
					var expectedSv1 = WindowBounds;
					var expectedSv2 = new Rect(-x + testCase.sv1X, -y + testCase.sv1Y, 512, 512);
					var expectedSut = testCase.empty
						? Empty
						: new Rect(
							Math.Max(0, -x + testCase.sv1X + testCase.sv2X),
							Math.Max(0, -y + testCase.sv1Y + testCase.sv2Y),
							Math.Min(512, 512 - x + testCase.sv1X + testCase.sv2X),
							Math.Min(512, 512 - y + testCase.sv1Y + testCase.sv2Y));

					vp1.Effective.Should().Be(expectedSv1, because: "sv1");
					vp2.Effective.Should().Be(expectedSv2, because: "sv2");
					vp.Effective.Should().Be(expectedSut, because: "sut");
				});
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_ItemsInSV()
		{
			/*
			<local:HeadedContent Header="Multi items in SV" Background="#FFA52C">
				<ScrollViewer>
					<StackPanel HorizontalAlignment="Stretch" Height="2048">
						<Border x:Name="item1" HorizontalAlignment="Center" Height="256" Width="100" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
						<Border x:Name="item2" HorizontalAlignment="Center" Height="256" Width="100" EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
					</StackPanel>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = true, canVerticallyScroll = true;
			Border item1, item2;
			ScrollViewer sv;

			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 256,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new StackPanel
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Height = 2048,
						Children =
						{
							(item1 = new Border
							{
								BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xA5, 0x2C)),
								BorderThickness = new Thickness(2),
								Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xA5, 0x2C)),
								Width = 100,
								Height = 512
							}),
							(item2 = new Border
							{
								BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xA5, 0x2C)),
								BorderThickness = new Thickness(2),
								Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xA5, 0x2C)),
								Width = 100,
								Height = 512
							})
						}
					}
				}
			};

			using var vp1 = VP(item1);
			using var vp2 = VP(item2);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert("only first visible", () =>
			{
				vp1.Effective.Should().Be(new Rect(0, 0, 512, 256));
				vp2.Effective.Should().Be(new Rect(0, -512, 512, 256));
			});

			sv.ChangeView(null, 512 - 128, null, disableAnimation: true);
			await RetryAssert("both visible", () =>
			{
				vp1.Effective.Should().Be(new Rect(0, 512 - 128, 512, 256));
				vp2.Effective.Should().Be(new Rect(0, -128, 512, 256));
			});

			sv.ChangeView(null, 1024 - 256, null, disableAnimation: true);
			await RetryAssert("only second visible", () =>
			{
				vp1.Effective.Should().Be(new Rect(0, 1024 - 256, 512, 256));
				vp2.Effective.Should().Be(new Rect(0, 512 - 256, 512, 256));
			});

			sv.ChangeView(null, sv.ScrollableHeight, null, disableAnimation: true);
			await RetryAssert("only second visible", () =>
			{
				vp1.Effective.Should().Be(new Rect(0, sv.ScrollableHeight, 512, 256));
				vp2.Effective.Should().Be(new Rect(0, sv.ScrollableHeight - 512, 512, 256));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_ClippedInSV()
		{
			/*
			<local:HeadedContent Header="Clipped in SV" Background="#FFFF41">
				<ScrollViewer>
					<Grid Height="1024">
						<Grid.Clip>
							<RectangleGeometry Rect="50,50,100,100"></RectangleGeometry>
						</Grid.Clip>
						<Border EffectiveViewportChanged="ShowEVP" Style="{StaticResource EVPContainer}"/>
					</Grid>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = true, canVerticallyScroll = true;
			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 512,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new Grid
					{
						Height = 1024,
						Clip = new RectangleGeometry { Rect = new Rect(50, 50, 100, 100) },
						Children =
						{
							(sut = new Border
							{
								Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x41)),
							})
						}
					}
				}
			};

			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(0, 0, 512, 512));
			});

			sv.ChangeView(null, verticalOffset: 512, null, disableAnimation: true);
			await WaitForIdle();

#if !__WASM__
			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(0, 512, 512, 512));
			});
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		[DataRow(0, 0)]
		[DataRow(1024, 0)]
		[DataRow(0, 1024)]
		[DataRow(1024, 1024)]
		public async Task EVP_When_Transform(int x, int y)
		{
			/*
			<local:HeadedContent Header="With transform" Background="#008018">
				<Border
					EffectiveViewportChanged="ShowEVP"
					Style="{StaticResource EVPContainer}"
					Width="100"
					Height="100"
					HorizontalAlignment="Left"
					VerticalAlignment="Top">
					<Border.RenderTransform>
						<TranslateTransform X="50" Y="25" />
					</Border.RenderTransform>
				</Border>
			</local:HeadedContent>
			*/

			Border sut;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = 512,
				Height = 512,
				Child = sut = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x80, 0x18)),
					Width = 100,
					Height = 100,
					Margin = new Thickness(x, y, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform { X = 50, Y = 25 }
				}
			};

			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-x - 50, -y - 25, WindowBounds.Width, WindowBounds.Height));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_TransformInSV()
		{
			/*
			<local:HeadedContent Header="With transform in SV" Background="#0000F9">
				<ScrollViewer>
					<Grid Height="1024">
						<Border
							EffectiveViewportChanged="ShowEVP"
							Style="{StaticResource EVPContainer}"
							Width="100"
							Height="100"
							HorizontalAlignment="Left"
							VerticalAlignment="Top">
							<Border.RenderTransform>
								<CompositeTransform TranslateX="50" TranslateY="25" />
							</Border.RenderTransform>
						</Border>
					</Grid>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = true, canVerticallyScroll = true;
			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 512,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new Grid
					{
						Height = 1024,
						Children =
						{
							(sut = new Border
							{
								HorizontalAlignment = HorizontalAlignment.Left,
								VerticalAlignment = VerticalAlignment.Top,
								Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xF9)),
								Width = 100,
								Height = 100,
								RenderTransform = new TranslateTransform { X = 50, Y = 25 }
							})
						}
					}
				}
			};

			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-50, -25, 512, 512));
			});

			sv.ChangeView(null, 512, null, disableAnimation: true);
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-50, 512 - 25, 512, 512));
			});
		}


		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task EVP_When_TransformWithScalingInSV()
		{
			/*
			<local:HeadedContent Header="Transform with scaling in SV" Background="#86007D">
				<ScrollViewer>
					<Grid Height="1024">
						<Border
							EffectiveViewportChanged="ShowEVP"
							Style="{StaticResource EVPContainer}"
							Width="100"
							Height="100"
							HorizontalAlignment="Left"
							VerticalAlignment="Top">
							<Border.RenderTransform>
								<CompositeTransform TranslateX="50" TranslateY="25" ScaleY="2" />
							</Border.RenderTransform>
						</Border>
					</Grid>
				</ScrollViewer>
			</local:HeadedContent>
			*/

			const bool canHorizontallyScroll = true, canVerticallyScroll = true;
			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 512,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new Grid
					{
						Height = 1024,
						Children =
						{
							(sut = new Border
							{
								HorizontalAlignment = HorizontalAlignment.Left,
								VerticalAlignment = VerticalAlignment.Top,
								Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xF9)),
								Width = 100,
								Height = 100,
								RenderTransform = new CompositeTransform { TranslateX = 50, TranslateY = 25, ScaleY = 2 }
							})
						}
					}
				}
			};

			using var vp = VP(sut);

			WindowContent = root;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-50, -25 / 2.0, 512, 256));
			});

			sv.ChangeView(null, 512, null, disableAnimation: true);
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Should().Be(new Rect(-50, (512 - 25) / 2.0, 512, 256));
			});
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !__IOS__
		[Ignore("This test native only element and is not supported on this platform")]
		public void EVP_When_NativeOnlyElement_Then_PassThrough() { }
#else
		public async Task EVP_When_NativeOnlyElement_Then_PassThrough()
		{
			Border sut;
			var tree = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = 512,
				Height = 512,
				Children =
				{
					new NativeOnlyElement
					{
						Child = (sut = new Border
						{
							HorizontalAlignment = HorizontalAlignment.Left,
							VerticalAlignment = VerticalAlignment.Top,
							Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xF9)),
							Width = 100,
							Height = 100,
						})
					}
				}
			};

			using var vp = VP(sut);

			WindowContent = tree;
			await WaitForIdle();

			await RetryAssert(() =>
			{
				vp.Effective.Width.Should().BeGreaterThan(100);
				vp.Effective.Height.Should().BeGreaterThan(100);
			});
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task EVP_When_RemoveHandlerWhileRaisingEvent()
		{
			const bool canHorizontallyScroll = true, canVerticallyScroll = true;
			Border sut;
			ScrollViewer sv;
			var root = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = sv = new ScrollViewer
				{
					Width = 512,
					Height = 512,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalScrollBarVisibility = canHorizontallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = canVerticallyScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					HorizontalScrollMode = canHorizontallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					VerticalScrollMode = canVerticallyScroll ? ScrollMode.Enabled : ScrollMode.Disabled,
					Content = new Grid
					{
						Height = 1024,
						Children =
						{
							(sut = new Border
							{
								HorizontalAlignment = HorizontalAlignment.Left,
								VerticalAlignment = VerticalAlignment.Top,
								Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xF9)),
								Width = 100,
								Height = 100,
								RenderTransform = new CompositeTransform { TranslateX = 50, TranslateY = 25, ScaleY = 2 }
							})
						}
					}
				}
			};

			var result = new TaskCompletionSource<object?>();
			var allowDetach = false;
			sut.EffectiveViewportChanged += OnSutEVPChanged;

			WindowContent = root;

			// First make sure to be loaded (and actually ignore them)
			await WaitForIdle();
			allowDetach = true;

			// Then cause a layout update
			sv.ChangeView(null, 512, null, disableAnimation: true);

			await result.Task;

			void OnSutEVPChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
			{
				try
				{
					if (!allowDetach)
					{
						return;
					}

					sut.EffectiveViewportChanged -= OnSutEVPChanged;
					result.TrySetResult(default);
				}
				catch (Exception e)
				{
					result.TrySetException(e);
				}
			}
		}

		private async Task RetryAssert(Action assertion)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					// On UWP we might not be scrolled properly to the right offset (even is we had disabled animation),
					// so we just retry assertion as long as it fails, for up to 300 ms.
					// Using the ViewChanged event is not reliable enough neither.

					assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= 30)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}

		private async Task RetryAssert(string scope, Action assertion)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					// On UWP we might not be scrolled properly to the right offset (even is we had disabled animation),
					// so we just retry assertion as long as it fails, for up to 300 ms.
					// Using the ViewChanged event is not reliable enough neither.

					using var _ = new AssertionScope(scope);
					assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= 30)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}

		private static double X(FrameworkElement element, double? available = null)
			=> element.HorizontalAlignment switch
			{
				HorizontalAlignment.Left => 0,
				HorizontalAlignment.Center => ((available ?? ((FrameworkElement)element.Parent).ActualWidth) - element.ActualWidth) / 2.0,
				HorizontalAlignment.Right => (available ?? ((FrameworkElement)element.Parent).ActualWidth) - element.ActualWidth,
				HorizontalAlignment.Stretch => ((available ?? ((FrameworkElement)element.Parent).ActualWidth) - element.ActualWidth) / 2.0,
				_ => 0
			};

		private static double Y(FrameworkElement element, double? available = null)
			=> element.VerticalAlignment switch
			{
				VerticalAlignment.Top => 0,
				VerticalAlignment.Center => ((available ?? ((FrameworkElement)element.Parent).ActualHeight) - element.ActualHeight) / 2.0,
				VerticalAlignment.Bottom => (available ?? ((FrameworkElement)element.Parent).ActualHeight) - element.ActualHeight,
				VerticalAlignment.Stretch => ((available ?? ((FrameworkElement)element.Parent).ActualHeight) - element.ActualHeight) / 2.0,
				_ => 0
			};

		private EVPListener VP(FrameworkElement elt)
			=> new EVPListener(elt);

		private EVPTreeListener VPTree(FrameworkElement root, FrameworkElement leaf)
			=> new EVPTreeListener(root, leaf);

		private class EVPListener : IDisposable
		{
			private readonly List<EffectiveViewportChangedEventArgs> _args = new List<EffectiveViewportChangedEventArgs>();
			private readonly FrameworkElement _elt;

			public EVPListener(FrameworkElement elt)
			{
				_elt = elt;
				elt.EffectiveViewportChanged += OnEVPChanged;
			}

			public Rect Effective => _args.Last().EffectiveViewport;

			private void OnEVPChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
				=> _args.Add(args);

			public void Dispose()
				=> _elt.EffectiveViewportChanged += OnEVPChanged;
		}

		private class EVPTreeListener : IDisposable
		{
			private readonly IDictionary<FrameworkElement, EVPListener> _listeners = new Dictionary<FrameworkElement, EVPListener>();
			private readonly FrameworkElement _root;
			private readonly FrameworkElement _leaf;

			public EVPTreeListener(FrameworkElement root, FrameworkElement leaf)
			{
				_root = root;
				_leaf = leaf;

				leaf.Loading += ElementLoading;
			}

			private void ElementLoading(FrameworkElement sender, object args)
			{
				_leaf.Loading -= ElementLoading;

				Subscribe(sender);

				void Subscribe(object elt)
				{
					if (elt is FrameworkElement fwElt)
					{
						_listeners[fwElt] = new EVPListener(fwElt);
					}

					if (Equals(_root, elt))
					{
						return;
					}

					if (VisualTreeHelper.GetParent(elt as DependencyObject) is { } parent)
					{
						Subscribe(parent);
					}
				}
			}

			public EVPListener this[FrameworkElement elt] => _listeners[elt];

			public EVPListener Of<T>()
				=> _listeners.Single(kvp => kvp.Key is T).Value;

			public void Dispose()
			{
				_leaf.Loading -= ElementLoading;
				foreach (var listener in _listeners.Values)
				{
					listener.Dispose();
				}
			}
		}

#if __IOS__
		public partial class NativeOnlyElement : UIKit.UIView
		{
			public NativeOnlyElement()
			{
				base.AutoresizingMask = UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight;
				base.AutosizesSubviews = true;
			}

			public UIElement? Child
			{
				get => Subviews.FirstOrDefault() as UIElement;
				set
				{
					foreach (var subview in Subviews)
					{
						subview.RemoveFromSuperview();
					}
					if (value is not null)
					{
						InsertSubview(value, 0);
					}
				}
			}
		}
#endif
	}
}
