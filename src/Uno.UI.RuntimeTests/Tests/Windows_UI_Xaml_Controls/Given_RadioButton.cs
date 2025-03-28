using System;
using System.Threading.Tasks;
using System.Windows.Input;
using FluentAssertions;
using Microsoft/* UWP don't rename */.UI.Xaml.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_RadioButton
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_GroupName_Default_Property_Value()
		{
			var radioButton = new RadioButton();
			radioButton.GroupName.Should().Be("");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GroupName_Default_Dependency_Property_Value()
		{
			var radioButton = new RadioButton();
			var value = radioButton.GetValue(RadioButton.GroupNameProperty);
			value.Should().BeNull();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GroupName_Set_Null()
		{
			var radioButton = new RadioButton();
			Action act = () => radioButton.GroupName = null;
			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Custom()
		{
			var radioButtonA1 = new RadioButton()
			{
				GroupName = "A"
			};
			var radioButtonA2 = new RadioButton()
			{
				GroupName = "A"
			};
			var radioButtonEmpty = new RadioButton()
			{
				GroupName = ""
			};
			var radioButtonB = new RadioButton()
			{
				GroupName = "B"
			};
			var container = new StackPanel()
			{
				Children =
				{
					radioButtonA1,
					radioButtonA2,
					radioButtonB,
					radioButtonEmpty
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from "A" group
			radioButtonA1.IsChecked = true;

			Assert.IsFalse(radioButtonA2.IsChecked);
			Assert.IsFalse(radioButtonB.IsChecked);
			Assert.IsFalse(radioButtonEmpty.IsChecked);

			// Check second from "A" group
			radioButtonA2.IsChecked = true;

			Assert.IsFalse(radioButtonA1.IsChecked);

			// Check other groups
			radioButtonB.IsChecked = true;
			radioButtonEmpty.IsChecked = true;

			Assert.IsTrue(radioButtonA2.IsChecked);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Custom_Two_Containers()
		{
			var radioButtonA1 = new RadioButton()
			{
				GroupName = "A"
			};
			var radioButtonA2 = new RadioButton()
			{
				GroupName = "A"
			};

			var container = new StackPanel()
			{
				Children =
				{
					new StackPanel()
					{
						Children =
						{
							radioButtonA1
						}
					},
					new StackPanel()
					{
						Children =
						{
							radioButtonA2
						}
					}
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from "A" group
			radioButtonA1.IsChecked = true;

			Assert.IsFalse(radioButtonA2.IsChecked);

			// Check second from "A" group
			radioButtonA2.IsChecked = true;

			Assert.IsFalse(radioButtonA1.IsChecked);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Empty()
		{
			var radioButtonA = new RadioButton()
			{
				GroupName = "A"
			};
			var radioButtonEmpty1 = new RadioButton()
			{
				GroupName = ""
			};
			var radioButtonEmpty2 = new RadioButton()
			{
				GroupName = ""
			};
			var radioButtonNull = new RadioButton();

			var container = new StackPanel()
			{
				Children =
				{
					radioButtonA,
					radioButtonEmpty1,
					radioButtonEmpty2,
					radioButtonNull
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from empty string group
			radioButtonEmpty1.IsChecked = true;

			Assert.IsFalse(radioButtonA.IsChecked);
			Assert.IsFalse(radioButtonEmpty2.IsChecked);
			Assert.IsFalse(radioButtonNull.IsChecked);

			// Check second from empty string group
			radioButtonEmpty2.IsChecked = true;

			Assert.IsFalse(radioButtonEmpty1.IsChecked);

			// Check "A" group
			radioButtonA.IsChecked = true;

			Assert.IsTrue(radioButtonEmpty2.IsChecked);

			// Check null group
			radioButtonNull.IsChecked = true;

			Assert.IsFalse(radioButtonEmpty2.IsChecked);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Empty_Two_Containers()
		{
			var radioButtonEmpty1 = new RadioButton()
			{
				GroupName = ""
			};
			var radioButtonEmpty2 = new RadioButton()
			{
				GroupName = ""
			};

			var container = new StackPanel()
			{
				Children =
				{
					new StackPanel()
					{
						Children =
						{
							radioButtonEmpty1
						}
					},
					new StackPanel()
					{
						Children =
						{
							radioButtonEmpty2
						}
					}
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from empty string group
			radioButtonEmpty1.IsChecked = true;

			Assert.IsFalse(radioButtonEmpty2.IsChecked);

			// Check second from empty string group
			radioButtonEmpty2.IsChecked = true;

			Assert.IsTrue(radioButtonEmpty1.IsChecked);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Default()
		{
			var radioButtonA = new RadioButton()
			{
				GroupName = "A"
			};
			var radioButtonNull1 = new RadioButton();
			var radioButtonNull2 = new RadioButton();
			var radioButtonEmpty = new RadioButton()
			{
				GroupName = ""
			};

			var container = new StackPanel()
			{
				Children =
				{
					radioButtonA,
					radioButtonNull1,
					radioButtonNull2,
					radioButtonEmpty
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from default group
			radioButtonNull1.IsChecked = true;

			Assert.IsFalse(radioButtonA.IsChecked);
			Assert.IsFalse(radioButtonNull2.IsChecked);
			Assert.IsFalse(radioButtonEmpty.IsChecked);

			// Check second from default group
			radioButtonNull2.IsChecked = true;

			Assert.IsFalse(radioButtonNull1.IsChecked);

			// Check "A" group
			radioButtonA.IsChecked = true;

			Assert.IsTrue(radioButtonNull2.IsChecked);

			// Check empty string group
			radioButtonEmpty.IsChecked = true;

			Assert.IsFalse(radioButtonNull2.IsChecked);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GroupName_Default_Two_Containers()
		{
			var radioButtonNull1 = new RadioButton();
			var radioButtonNull2 = new RadioButton();
			var radioButtonEmpty = new RadioButton()
			{
				GroupName = ""
			};

			var container = new StackPanel()
			{
				Children =
				{
					new StackPanel()
					{
						Children =
						{
							radioButtonNull1
						}
					},
					new StackPanel()
					{
						Children =
						{
							radioButtonNull2,
							radioButtonEmpty
						}
					}
				}
			};

			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			// Check first from default group
			radioButtonNull1.IsChecked = true;

			Assert.IsFalse(radioButtonNull2.IsChecked);

			// Check second from default group
			radioButtonNull2.IsChecked = true;

			Assert.IsTrue(radioButtonNull1.IsChecked);

			// Check from empty string radio
			radioButtonEmpty.IsChecked = true;

			Assert.IsFalse(radioButtonNull2.IsChecked);
		}

		[TestMethod]
		public async Task When_AutomationPeer_Toggle()
		{
			RadioButton radioButton = null;
			await RunOnUIThread(() =>
			{
				radioButton = new RadioButton();
				WindowHelper.WindowContent = radioButton;
			});

			await WindowHelper.WaitForIdle();

			// EventTester shouldn't run on UI thread as it will end up calling `this.CaptureScreenAsync("Before").Wait(this.Timeout)`
			// If EventTester is run on UI thread, the Wait call is going to block the UI thread, but CaptureScreenAsync needs the UI thread to complete.
			// So, it's going to timeout. Note that when the debugger is attached, Timeout is infinite.
			using (var clickEvent = new EventTester<RadioButton, RoutedEventArgs>(radioButton, "Click"))
			{
				await RunOnUIThread(async () =>
				{
					var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton) as RadioButtonAutomationPeer;
					peer.Toggle();

					Assert.IsTrue(await clickEvent.WaitAsync(TimeSpan.FromSeconds(3)));
				});
			}
		}

		[TestMethod]
		public async Task When_AutomationPeer_Toggle_With_Command()
		{
			RadioButton radioButton = null;
			TestCommand command = null;
			await RunOnUIThread(() =>
			{
				radioButton = new RadioButton();
				command = new TestCommand();
				radioButton.Command = command;
				WindowHelper.WindowContent = radioButton;
			});

			await WindowHelper.WaitForIdle();

			// EventTester shouldn't run on UI thread as it will end up calling `this.CaptureScreenAsync("Before").Wait(this.Timeout)`
			// If EventTester is run on UI thread, the Wait call is going to block the UI thread, but CaptureScreenAsync needs the UI thread to complete.
			// So, it's going to timeout. Note that when the debugger is attached, Timeout is infinite.
			using (var commandFired = new EventTester<TestCommand, EventArgs>(command, "CommandFired"))
			{
				await RunOnUIThread(async () =>
				{
					var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton) as RadioButtonAutomationPeer;
					peer.Toggle();

					Assert.IsTrue(await commandFired.WaitAsync(TimeSpan.FromSeconds(3)));
				});
			}
		}
	}

	public class TestCommand : ICommand
	{
		public event EventHandler CanExecuteChanged { add { } remove { } }
		public event EventHandler CommandFired;

		public TestCommand()
		{
		}

		public bool CanExecute(object o)
		{
			return true;
		}

		public void Execute(object o) => CommandFired.Invoke(this, null);
	}
}
