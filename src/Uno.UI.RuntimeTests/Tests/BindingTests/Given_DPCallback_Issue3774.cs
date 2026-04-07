using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

// Repro tests for https://github.com/unoplatform/uno/issues/3774

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_DPCallback_Issue3774
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3774")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_DP_Callback_Fires_TwoWay_Binding_Already_Updated()
	{
		// Issue: DP callbacks are invoked before 2-way bindings are updated.
		// When a DP value changes (e.g., slider value), the callback fires
		// and at that point the bound ViewModel property should already have the new value.
		// Expected: At callback time, ViewModel.Value == slider.Value (binding already pushed).
		// Bug: At callback time, ViewModel.Value was still the old value.

		var vm = new VM3774 { Value = 0 };

		double? vmValueAtCallbackTime = null;
		double? dpValueAtCallbackTime = null;

		var slider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 0,
		};

		// Set up the 2-way binding
		slider.SetBinding(Slider.ValueProperty, new Binding
		{
			Path = new PropertyPath(nameof(vm.Value)),
			Mode = BindingMode.TwoWay,
			Source = vm,
		});

		// Register a DP callback to capture values at callback time
		slider.RegisterPropertyChangedCallback(Slider.ValueProperty, (s, dp) =>
		{
			vmValueAtCallbackTime = vm.Value;
			dpValueAtCallbackTime = ((Slider)s).Value;
		});

		WindowHelper.WindowContent = slider;
		await WindowHelper.WaitForLoaded(slider);
		await WindowHelper.WaitForIdle();

		// Change the DP value to trigger the callback
		slider.Value = 42;
		await WindowHelper.WaitForIdle();

		Assert.IsNotNull(vmValueAtCallbackTime, "Callback should have fired.");
		Assert.IsNotNull(dpValueAtCallbackTime, "Callback should have fired.");

		// At callback time, the ViewModel should already have been updated (TwoWay binding)
		Assert.AreEqual(dpValueAtCallbackTime, vmValueAtCallbackTime,
			$"When DP callback fires, ViewModel.Value ({vmValueAtCallbackTime}) should already equal " +
			$"the new DP value ({dpValueAtCallbackTime}). " +
			$"This confirms DP callbacks fire before 2-way binding updates.");
	}

	private class VM3774 : INotifyPropertyChanged
	{
		private double _value;
		public double Value
		{
			get => _value;
			set
			{
				_value = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
