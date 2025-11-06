using System;
using System.Diagnostics.CodeAnalysis;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Markup
{
	/// <summary>
	/// Provides helper methods for data binding.
	/// </summary>
	public sealed partial class XamlBindingHelper
	{
		private static readonly Action ResumeRenderingOnlyOnFrameworkElement =
			Actions.CreateOnce(() => typeof(XamlBindingHelper).Log().Error("ResumeRendering/SuspendRendering is only supported on FrameworkElement instances."));

		/// <summary>
		/// Converts a value from a source type to a target type.
		/// </summary>
		public static object ConvertValue(
			Type type,
			object value)
			=> Uno.UI.DataBinding.BindingPropertyHelper.Convert(type, value);

		/// <summary>
		/// Resumes rendering of the specified element.
		/// </summary>
		/// <param name="target"></param>
		public static void ResumeRendering(UIElement target)
		{
			if (target is FrameworkElement fe)
			{
				fe.ResumeRendering();
			}
			else
			{
				ResumeRenderingOnlyOnFrameworkElement();
			}
		}

		/// <summary>
		/// Suspends rendering of the specified element.
		/// </summary>
		/// <param name="target"></param>
		public static void SuspendRendering(UIElement target)
		{
			if (target is FrameworkElement fe)
			{
				fe.SuspendRendering();
			}
			else
			{
				ResumeRenderingOnlyOnFrameworkElement();
			}
		}

		public static void SetPropertyFromBoolean(object dependencyObject, DependencyProperty propertyToSet, bool value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromByte(object dependencyObject, DependencyProperty propertyToSet, byte value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromChar16(object dependencyObject, DependencyProperty propertyToSet, char value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromDateTime(object dependencyObject, DependencyProperty propertyToSet, DateTimeOffset value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromDouble(object dependencyObject, DependencyProperty propertyToSet, double value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromInt32(object dependencyObject, DependencyProperty propertyToSet, int value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromInt64(object dependencyObject, DependencyProperty propertyToSet, long value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromObject(object dependencyObject, DependencyProperty propertyToSet, object value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromPoint(object dependencyObject, DependencyProperty propertyToSet, Point value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromRect(object dependencyObject, DependencyProperty propertyToSet, Rect value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromSingle(object dependencyObject, DependencyProperty propertyToSet, float value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromSize(object dependencyObject, DependencyProperty propertyToSet, global::Windows.Foundation.Size value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromString(object dependencyObject, DependencyProperty propertyToSet, string value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromTimeSpan(object dependencyObject, DependencyProperty propertyToSet, TimeSpan value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromUInt32(object dependencyObject, DependencyProperty propertyToSet, uint value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromUInt64(object dependencyObject, DependencyProperty propertyToSet, ulong value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);

		public static void SetPropertyFromUri(object dependencyObject, DependencyProperty propertyToSet, Uri value) =>
			(dependencyObject as DependencyObject).SetValue(propertyToSet, value);
	}
}
