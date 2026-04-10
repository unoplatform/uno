using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid)]
		public async Task When_RadioButtons_Unload_SelectedIndex()
		{
			var vm = new When_RadioButtons_Unload_VM
			{
				Items = ["QweQwe", "AsdAsd", "ZxcZxc"],
				CurrentIndex = 1,
			};

			var sut = new RadioButtons
			{
				ItemsSource = vm.Items,
			};
#if DEBUG
			// note: remember that with #22479 resetting data-bound properties on flyout closing,
			// observing with a breakpoint will have a side-effect: bp hit > app lost focus > flyout is closed > bug resetting radio dp
			vm.PropertyChangingEx += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"VM::{e.Property}: {e.OldValue} -> {e.NewValue}");

				if (e is { Property: nameof(When_RadioButtons_Unload_VM.CurrentIndex), NewValue: -1 }) { }
			};
#endif
			sut.SetBinding(RadioButtons.SelectedIndexProperty, new Binding
			{
				Path = new PropertyPath(nameof(When_RadioButtons_Unload_VM.CurrentIndex)),
				Mode = BindingMode.TwoWay,
				Source = vm,
			});
			var flyout = new Flyout
			{
				Content = new Grid { Children = { sut } }
			};
			var dropdown = new DropDownButton
			{
				Content = "sadsadasd",
				Flyout = flyout,
			};

			WindowHelper.WindowContent = dropdown;
			await WindowHelper.WaitForLoaded(dropdown);

			try
			{
				// 0. Open the flyout —- initial selection should be @1
				flyout.ShowAt(dropdown);
				await WindowHelper.WaitForLoaded(sut);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, sut.SelectedIndex, "0. Initial 'sut.SelectedIndex' should be 1");
				Assert.AreEqual(1, vm.CurrentIndex, "0. Initial 'vm.CurrentIndex' should be 1");

				// 1. Select the last item @2 programmatically -- ...
				sut.SelectedIndex = 2;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(2, vm.CurrentIndex, "1. 'vm.CurrentIndex' should've been changed to 2");

				// 2. Close the flyout —- selection should be preserved
				flyout.Hide();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(2, vm.CurrentIndex, "2. 'vm.CurrentIndex' should be preserved as 2");

				// 3. Re-open the flyout —- selection should remain unchanged
				flyout.ShowAt(dropdown);
				await WindowHelper.WaitForLoaded(sut);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(2, sut.SelectedIndex, "3. 'sut.SelectedIndex' should remain unchanged as 2");
				Assert.AreEqual(2, vm.CurrentIndex, "3. 'vm.CurrentIndex' should remain unchanged as 2");
			}
			finally
			{
				UITestHelper.CloseAllPopups();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid)]
		public async Task When_RadioButtons_Unload_SelectedItem()
		{
			var vm = new When_RadioButtons_Unload_VM();
			{
				vm.Items = ["QweQwe", "AsdAsd", "ZxcZxc"];
				vm.CurrentItem = vm.Items[1];
			}

			var sut = new RadioButtons
			{
				ItemsSource = vm.Items,
			};
#if DEBUG
			// note: remember that with #22479 resetting data-bound properties on flyout closing,
			// observing with a breakpoint will have a side-effect: bp hit > app lost focus > flyout is closed > bug resetting radio dp
			vm.PropertyChangingEx += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"VM::{e.Property}: {e.OldValue} -> {e.NewValue}");

				if (e is { Property: nameof(When_RadioButtons_Unload_VM.CurrentItem), NewValue: null }) { }
			};
#endif
			sut.SetBinding(RadioButtons.SelectedItemProperty, new Binding
			{
				Path = new PropertyPath(nameof(When_RadioButtons_Unload_VM.CurrentItem)),
				Mode = BindingMode.TwoWay,
				Source = vm,
			});
			var flyout = new Flyout
			{
				Content = new Grid { Children = { sut } }
			};
			var dropdown = new DropDownButton
			{
				Content = "sadsadasd",
				Flyout = flyout,
			};

			WindowHelper.WindowContent = dropdown;
			await WindowHelper.WaitForLoaded(dropdown);

			try
			{
				// 0. Open the flyout —- initial selection should be Items@1
				flyout.ShowAt(dropdown);
				await WindowHelper.WaitForLoaded(sut);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(vm.Items[1], sut.SelectedItem, $"0. Initial 'sut.SelectedItem' should be '{vm.Items[1]}'");
				Assert.AreEqual(vm.Items[1], vm.CurrentItem, $"0. Initial 'vm.CurrentItem' should be '{vm.Items[1]}'");

				// 1. Select the last item @2 programmatically --- ...
				sut.SelectedItem = vm.Items[2];
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(vm.Items[2], vm.CurrentItem, $"1. 'vm.CurrentItem' should've been changed to '{vm.Items[2]}'");

				// 2. Close the flyout —- selection should be preserved
				flyout.Hide();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(vm.Items[2], vm.CurrentItem, $"2. 'vm.CurrentItem' should be preserved as '{vm.Items[2]}'");

				// 3. Re-open the flyout —- selection should remain unchanged
				flyout.ShowAt(dropdown);
				await WindowHelper.WaitForLoaded(sut);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(vm.Items[2], sut.SelectedItem, $"3. 'sut.SelectedItem' should remain unchanged as '{vm.Items[2]}'");
				Assert.AreEqual(vm.Items[2], vm.CurrentItem, $"3. 'vm.CurrentItem' should remain unchanged as '{vm.Items[2]}'");
			}
			finally
			{
				UITestHelper.CloseAllPopups();
			}
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

	public class When_RadioButtons_Unload_VM : INotifyPropertyChanged//, INotifyPropertyChanging
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event TypedEventHandler<object, (string Property, object OldValue, object NewValue)> PropertyChangingEx;

		private int _currentIndex;
		private object _currentItem;
		private ObservableCollection<string> _items;

		public int CurrentIndex
		{
			get => _currentIndex;
			set
			{
				PropertyChangingEx?.Invoke(this, (nameof(CurrentIndex), _currentIndex, value));
				_currentIndex = value;
				PropertyChanged?.Invoke(this, new(nameof(CurrentIndex)));
			}
		}
		public object CurrentItem
		{
			get => _currentItem;
			set
			{
				PropertyChangingEx?.Invoke(this, (nameof(CurrentItem), _currentItem, value));
				_currentItem = value;
				PropertyChanged?.Invoke(this, new(nameof(CurrentItem)));
			}
		}
		public ObservableCollection<string> Items
		{
			get => _items;
			set
			{
				PropertyChangingEx?.Invoke(this, (nameof(Items), _items, value));
				_items = value;
				PropertyChanged?.Invoke(this, new(nameof(Items)));
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
