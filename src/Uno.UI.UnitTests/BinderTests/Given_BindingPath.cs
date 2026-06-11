using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_BindingPath
	{
		[TestMethod]
		public void When_SetValue_Then_TargetUpdated()
		{
			var (target, sut) = Arrange();

			sut.Value = "42";

			Assert.AreEqual("42", target.Value);
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(forAnimations: true);

			sut.Value = "Animations"; // Animations

			Assert.AreEqual("Animations", target.Value);
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValueAndClear_SetTargetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(forAnimations: true);

			sut.Value = "Animations";
			sut.ClearValue();

			target.Value = "TargetLocalValue";

			Assert.AreEqual("TargetLocalValue", target.Value);
		}

		[TestMethod]
		public void When_Parse_SimpleProperty()
		{
			var sut = new BindingPath("hello_world", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(1);
			result[0].PropertyName.Should().Be("hello_world");
		}

		[TestMethod]
		public void When_Parse_SimpleProperties()
		{
			var sut = new BindingPath("hello.world.bonjour.le.monde", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(5);
			result[0].PropertyName.Should().Be("hello");
			result[1].PropertyName.Should().Be("world");
			result[2].PropertyName.Should().Be("bonjour");
			result[3].PropertyName.Should().Be("le");
			result[4].PropertyName.Should().Be("monde");
		}

		[TestMethod]
		public void When_Parse_AttachedProperty()
		{
			var sut = new BindingPath("(Grid.Column)", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(1);
			result[0].PropertyName.Should().Be("Grid.Column");
		}

		[TestMethod]
		public void When_Parse_AttachedProperties()
		{
			var sut = new BindingPath("(hello.world).(bonjour:le.monde)", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(2);
			result[0].PropertyName.Should().Be("hello.world");
			result[1].PropertyName.Should().Be("bonjour:le.monde");
		}

		[TestMethod]
		public void When_Parse_Indexer()
		{
			var sut = new BindingPath("[hello_world]", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(1);
			result[0].PropertyName.Should().Be("[hello_world]");
		}

		[TestMethod]
		public void When_Parse_Indexers()
		{
			var sut = new BindingPath("[hello][world][bonjour][le][monde]", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(5);
			result[0].PropertyName.Should().Be("[hello]");
			result[1].PropertyName.Should().Be("[world]");
			result[2].PropertyName.Should().Be("[bonjour]");
			result[3].PropertyName.Should().Be("[le]");
			result[4].PropertyName.Should().Be("[monde]");
		}

		[TestMethod]
		public void When_Parse_ComplexPath()
		{
			var sut = new BindingPath("hello[world](bonjour:le.monde).value", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(4);
			result[0].PropertyName.Should().Be("hello");
			result[1].PropertyName.Should().Be("[world]");
			result[2].PropertyName.Should().Be("bonjour:le.monde");
			result[3].PropertyName.Should().Be("value");
		}

		[TestMethod]
		public void When_Parse_TrimItemPath()
		{
			var sut = new BindingPath(" hello [world ]( bonjour:le.monde ).value ", null);

			var result = sut.GetPathItems().ToArray();

			result.Length.Should().Be(4);
			result[0].PropertyName.Should().Be("hello");
			result[1].PropertyName.Should().Be("[world ]");
			result[2].PropertyName.Should().Be("bonjour:le.monde");
			result[3].PropertyName.Should().Be("value");
		}

		private static (MyTarget target, BindingPath binding) Arrange(bool forAnimations = false)
		{
			var target = new MyTarget();
			var binding = new BindingPath(nameof(MyTarget.Value), MyTarget.FallbackValue, forAnimations, false)
			{
				DataContext = target
			};

			return (target, binding);
		}

		private static (object target, BindingPath binding) ArrangeIncorrect(bool forAnimations = false)
		{
			var target = new object();
			var binding = new BindingPath(nameof(MyTarget.Value), MyTarget.FallbackValue, forAnimations, false)
			{
				DataContext = target
			};

			return (target, binding);
		}

		public partial class MyTarget : DependencyObject
		{
			public const string DefaultValue = "__default__";
			public const string FallbackValue = "__fallback__";

			public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
				"Value", typeof(string), typeof(MyTarget), new FrameworkPropertyMetadata(DefaultValue));

			public string Value
			{
				get => (string)this.GetValue(ValueProperty);
				set => this.SetValue(ValueProperty, value);
			}
		}
	}
}
