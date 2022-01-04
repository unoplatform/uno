using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public class CreateFromStringBase
	{
		public CreateFromStringBase(int value)
		{
			Value = value;
		}

		public int Value { get; set; }
	}

	[CreateFromString(MethodName = "??????")]
	public class CreateFromStringInvalidMethodName : CreateFromStringBase
	{
		public CreateFromStringInvalidMethodName(int value) : base(value)
		{
		}
	}

	public partial class CreateFromStringInvalidMethodNameOwner : FrameworkElement
	{
		public CreateFromStringInvalidMethodName Test
		{
			get => (CreateFromStringInvalidMethodName)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringInvalidMethodName), typeof(CreateFromStringInvalidMethodNameOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringNonQualifiedMethodName : CreateFromStringBase
	{
		public CreateFromStringNonQualifiedMethodName(int value) : base(value)
		{
		}

		public static CreateFromStringNonQualifiedMethodName ConversionMethod(string input)
		{
			return new CreateFromStringNonQualifiedMethodName(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringNonQualifiedMethodNameOwner : FrameworkElement
	{
		public CreateFromStringNonQualifiedMethodName Test
		{
			get => (CreateFromStringNonQualifiedMethodName)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringNonQualifiedMethodName), typeof(CreateFromStringNonQualifiedMethodNameOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests.CreateFromStringFullyQualifiedMethodName.ConversionMethod")]
	public class CreateFromStringFullyQualifiedMethodName : CreateFromStringBase
	{
		public CreateFromStringFullyQualifiedMethodName(int value) : base(value)
		{
		}

		public static CreateFromStringFullyQualifiedMethodName ConversionMethod(string input)
		{
			return new CreateFromStringFullyQualifiedMethodName(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringFullyQualifiedMethodNameOwner : FrameworkElement
	{
		public CreateFromStringFullyQualifiedMethodName Test
		{
			get => (CreateFromStringFullyQualifiedMethodName)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringFullyQualifiedMethodName), typeof(CreateFromStringFullyQualifiedMethodNameOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringNonStaticMethod : CreateFromStringBase
	{
		public CreateFromStringNonStaticMethod(int value) : base(value)
		{
		}

		public CreateFromStringBase ConversionMethod(string input)
		{
			return new CreateFromStringNonStaticMethod(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringNonStaticMethodOwner : FrameworkElement
	{
		public CreateFromStringNonStaticMethod Test
		{
			get => (CreateFromStringNonStaticMethod)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringNonStaticMethod), typeof(CreateFromStringNonStaticMethodOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringPrivateStaticMethod : CreateFromStringBase
	{
		public CreateFromStringPrivateStaticMethod(int value) : base(value)
		{
		}

		private static CreateFromStringPrivateStaticMethod ConversionMethod(string input)
		{
			return new CreateFromStringPrivateStaticMethod(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringPrivateStaticMethodOwner : FrameworkElement
	{
		public CreateFromStringPrivateStaticMethod Test
		{
			get => (CreateFromStringPrivateStaticMethod)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringPrivateStaticMethod), typeof(CreateFromStringPrivateStaticMethodOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringInternalStaticMethod : CreateFromStringBase
	{
		public CreateFromStringInternalStaticMethod(int value) : base(value)
		{
		}

		public static CreateFromStringInternalStaticMethod ConversionMethod(string input)
		{
			return new CreateFromStringInternalStaticMethod(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringInternalStaticMethodOwner : FrameworkElement
	{
		public CreateFromStringInternalStaticMethod Test
		{
			get => (CreateFromStringInternalStaticMethod)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringInternalStaticMethod), typeof(CreateFromStringInternalStaticMethodOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringInvalidParameters : CreateFromStringBase
	{
		public CreateFromStringInvalidParameters(int value) : base(value)
		{
		}

		public static CreateFromStringInvalidParameters ConversionMethod(string input, object args)
		{
			return new CreateFromStringInvalidParameters(int.Parse(input) * 2);
		}
	}

	public partial class CreateFromStringInvalidParametersOwner : FrameworkElement
	{
		public CreateFromStringInvalidParameters Test
		{
			get => (CreateFromStringInvalidParameters)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringInvalidParameters), typeof(CreateFromStringInvalidParametersOwner), new PropertyMetadata(null));
	}

	[CreateFromString(MethodName = "ConversionMethod")]
	public class CreateFromStringInvalidReturnType : CreateFromStringBase
	{
		public CreateFromStringInvalidReturnType(int value) : base(value)
		{
		}

		public static double ConversionMethod(string input)
		{
			return int.Parse(input) * 2.0;
		}
	}

	public partial class CreateFromStringInvalidReturnTypeOwner : FrameworkElement
	{
		public CreateFromStringInvalidReturnType Test
		{
			get => (CreateFromStringInvalidReturnType)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(CreateFromStringInvalidReturnType), typeof(CreateFromStringInvalidReturnTypeOwner), new PropertyMetadata(null));
	}
}
