using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	/// <summary>
	/// Asserts WinUI's behavior for elements that are not in the live visual tree:
	/// they are not focusable, so <see cref="Control.Focus(FocusState)"/> fails and
	/// focus-candidate searches skip them (CUIElement::IsFocusable requires IsActive()).
	/// </summary>
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeMobile)]
	public class Given_FocusManager_DetachedElements
	{
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focus_Detached_Element_Should_Fail()
		{
			var attachedButton = new Button { Content = "Attached" };
			var removedButton = new Button { Content = "Removed" };
			var root = new StackPanel
			{
				Children = { attachedButton, removedButton }
			};
			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(attachedButton);

				root.Children.Remove(removedButton);
				await TestServices.WindowHelper.WaitForIdle();

				var neverAttachedButton = new Button { Content = "Never attached" };

				Assert.IsFalse(neverAttachedButton.Focus(FocusState.Programmatic));
				Assert.IsFalse(removedButton.Focus(FocusState.Programmatic));
				Assert.AreEqual(attachedButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_FindFirstFocusableElement_On_Detached_Subtree_Should_Return_Null()
		{
			var attachedButton = new Button { Content = "Attached" };
			var removedSubtree = new StackPanel
			{
				Children =
				{
					new Button { Content = "Removed" }
				}
			};
			var root = new StackPanel
			{
				Children = { attachedButton, removedSubtree }
			};
			try
			{
				await UITestHelper.Load(root);
				root.Children.Remove(removedSubtree);
				await TestServices.WindowHelper.WaitForIdle();

				var neverAttachedSubtree = new StackPanel
				{
					Children =
					{
						new Button { Content = "Never attached" }
					}
				};

				Assert.IsNull(FocusManager.FindFirstFocusableElement(neverAttachedSubtree));
				Assert.IsNull(FocusManager.FindLastFocusableElement(neverAttachedSubtree));
				Assert.IsNull(FocusManager.FindFirstFocusableElement(removedSubtree));
				Assert.IsNull(FocusManager.FindLastFocusableElement(removedSubtree));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focused_Loaded_Control_Is_Removed_Should_Not_Remain_Focused()
		{
			var removedButton = new Button { Content = "Removed" };
			var nextButton = new Button { Content = "Next" };
			var root = new StackPanel
			{
				Children = { removedButton, nextButton }
			};
			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(removedButton);

				root.Children.Remove(removedButton);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreNotEqual(removedButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focused_TextBlock_Is_Removed_Should_Not_Remain_Focused()
		{
			var removedTextBlock = new TextBlock
			{
				IsTabStop = true,
				Text = "Removed"
			};
			var root = new StackPanel
			{
				Children = { removedTextBlock }
			};
			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(removedTextBlock);

				root.Children.Remove(removedTextBlock);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(removedTextBlock.Focus(FocusState.Programmatic));
				Assert.AreNotEqual(removedTextBlock, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focused_Element_Is_Removed_LosingFocus_Cannot_Cancel_Or_Redirect()
		{
			var removedTextBlock = new TextBlock
			{
				IsTabStop = true,
				Text = "Removed"
			};
			var root = new StackPanel
			{
				Children = { removedTextBlock }
			};
			var cancelAttempted = false;
			var cancellationAccepted = false;
			var redirectionAccepted = false;
			removedTextBlock.LosingFocus += OnLosingFocus;
			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(removedTextBlock);

				root.Children.Remove(removedTextBlock);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(cancelAttempted);
				Assert.IsFalse(cancellationAccepted);
				Assert.IsFalse(redirectionAccepted);
				Assert.AreNotEqual(removedTextBlock, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				removedTextBlock.LosingFocus -= OnLosingFocus;
				TestServices.WindowHelper.WindowContent = null;
			}

			void OnLosingFocus(UIElement sender, LosingFocusEventArgs args)
			{
				cancelAttempted = true;
				cancellationAccepted = args.TryCancel();
				redirectionAccepted = args.TrySetNewFocusedElement(removedTextBlock);
				args.Cancel = true;
				args.NewFocusedElement = removedTextBlock;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focus_Target_Is_Removed_Should_Not_Steal_Focus()
		{
			// A dialog confining focus with TabFocusNavigation=Cycle, plus a sibling
			// subtree that gets removed while a callback still holds onto it. The
			// subsequent Focus() simulates that stale callback and must be a no-op.
			var okButton = new Button { Content = "OK" };
			var dialog = new StackPanel
			{
				TabFocusNavigation = KeyboardNavigationMode.Cycle,
				Children = { okButton }
			};

			var strayButton = new Button { Content = "Stray" };
			var strayPanel = new StackPanel
			{
				Children = { strayButton }
			};

			var root = new Grid
			{
				Children = { dialog, strayPanel }
			};

			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(okButton);

				root.Children.Remove(strayPanel);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(strayButton.Focus(FocusState.Programmatic));
				Assert.AreEqual(okButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focused_Control_Is_Removed_Before_Loaded_Should_Not_Remain_Focused()
		{
			var attachedButton = new Button { Content = "Attached" };
			var root = new StackPanel
			{
				Children = { attachedButton }
			};
			try
			{
				await UITestHelper.Load(root);
				await FocusWithRetriesAsync(attachedButton);

				var removedButton = new Button { Content = "Removed" };
				root.Children.Add(removedButton);
				Assert.IsTrue(removedButton.Focus(FocusState.Programmatic));

				root.Children.Remove(removedButton);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreNotEqual(removedButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Focus_Detached_Hyperlink_Should_Fail()
		{
			var attachedButton = new Button { Content = "Attached" };
			var hyperlink = new Hyperlink
			{
				Inlines =
				{
					new Run { Text = "Removed link" }
				}
			};
			var textBlock = new TextBlock
			{
				Inlines = { hyperlink }
			};
			var root = new StackPanel
			{
				Children = { attachedButton, textBlock }
			};
			try
			{
				await UITestHelper.Load(root);
				Assert.IsTrue(hyperlink.Focus(FocusState.Programmatic));
				Assert.AreEqual(hyperlink, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

				root.Children.Remove(textBlock);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreNotEqual(hyperlink, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
				await FocusWithRetriesAsync(attachedButton);
				Assert.IsFalse(hyperlink.Focus(FocusState.Programmatic));
				Assert.AreEqual(attachedButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		[DataRow("Remove")]
		[DataRow("Clear")]
		[DataRow("Replace")]
		public async Task When_Focused_Hyperlink_Is_Removed_From_InlineCollection_Should_Not_Remain_Focused(string operation)
		{
			var hyperlink = new Hyperlink
			{
				Inlines =
				{
					new Run { Text = "Removed link" }
				}
			};
			var textBlock = new TextBlock
			{
				Inlines = { hyperlink }
			};
			try
			{
				await UITestHelper.Load(textBlock);
				Assert.IsTrue(hyperlink.Focus(FocusState.Programmatic));
				Assert.AreEqual(hyperlink, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

				switch (operation)
				{
					case "Remove":
						Assert.IsTrue(textBlock.Inlines.Remove(hyperlink));
						break;
					case "Clear":
						textBlock.Inlines.Clear();
						break;
					case "Replace":
						textBlock.Inlines[0] = new Run { Text = "Replacement" };
						break;
					default:
						Assert.Fail($"Unknown operation: {operation}");
						break;
				}

				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreNotEqual(hyperlink, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
				Assert.IsFalse(hyperlink.Focus(FocusState.Programmatic));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_MenuFlyout_Item_Is_Closed_Should_Not_Accept_Focus()
		{
			var owner = new Button { Content = "Owner" };
			var item = new MenuFlyoutItem { Text = "Item" };
			var flyout = new MenuFlyout
			{
				Items = { item }
			};
			try
			{
				await UITestHelper.Load(owner);
				flyout.ShowAt(owner);
				await UITestHelper.WaitForLoaded(item);

				flyout.Hide();
				await TestServices.WindowHelper.WaitForIdle();
				await FocusWithRetriesAsync(owner);

				Assert.IsFalse(item.Focus(FocusState.Programmatic));
				Assert.AreEqual(owner, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				flyout.Hide();
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23750")]
		public async Task When_Closed_Flyout_Content_Has_Active_Logical_Owner_Should_Not_Accept_Focus()
		{
			var flyoutContent = new Button { Content = "Flyout content" };
			var flyout = new Flyout { Content = flyoutContent };
			var owner = new Button
			{
				Content = "Owner",
				Flyout = flyout
			};
			try
			{
				await UITestHelper.Load(owner);
				await FocusWithRetriesAsync(owner);

				Assert.IsFalse(flyoutContent.Focus(FocusState.Programmatic));
				Assert.AreEqual(owner, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			}
			finally
			{
				flyout.Hide();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static async Task FocusWithRetriesAsync(UIElement element)
		{
			if (!element.Focus(FocusState.Programmatic))
			{
				await TestServices.WindowHelper.WaitFor(
					() => element.Focus(FocusState.Programmatic),
					timeoutMS: 5000,
					message: "Could not focus the attached element");
			}

			Assert.AreEqual(element, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
		}
	}
}
