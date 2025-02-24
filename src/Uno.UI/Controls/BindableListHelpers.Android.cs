using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public static class BindableListViewHelpers
	{
		public static int ReadAttributeValue(Android.Content.Context context, IAttributeSet attrs, int[] groupId,
											 int requiredAttributeId)
		{
			var typedArray = context.ObtainStyledAttributes(attrs, groupId);

			try
			{
				var numStyles = typedArray.IndexCount;
				for (var i = 0; i < numStyles; ++i)
				{
					var attributeId = typedArray.GetIndex(i);
					if (attributeId == requiredAttributeId)
					{
						return typedArray.GetResourceId(attributeId, 0);
					}
				}
				return 0;
			}
			finally
			{
				typedArray.Recycle();
			}
		}

		public static int[] ReadAttributeValue(Android.Content.Context context, IAttributeSet attrs, int[] groupId,
											 int[] requiredAttributeId)
		{
			var typedArray = context.ObtainStyledAttributes(attrs, groupId);

			try
			{
				var attributes = Enumerable
						.Range(0, typedArray.IndexCount)
						.Select(i => (int?)typedArray.GetIndex(i))
						.ToArray();

				var q = from reqId in requiredAttributeId
						let foundAttributeId = attributes.FirstOrDefault(a => a == reqId)
						select foundAttributeId != null ? typedArray.GetResourceId(foundAttributeId.Value, 0) : 0;

				return q.ToArray();
			}
			finally
			{
				typedArray.Recycle();
			}
		}

		public static string ReadStringValue(Android.Content.Context context, IAttributeSet attrs, int[] groupId, int requiredAttributeId)
		{
			var typedArray = context.ObtainStyledAttributes(attrs, groupId);

			try
			{
				var numStyles = typedArray.IndexCount;
				for (var i = 0; i < numStyles; ++i)
				{
					var attributeId = typedArray.GetIndex(i);
					if (attributeId == requiredAttributeId)
					{
						return typedArray.GetString(i);
					}
				}

				return string.Empty;
			}
			finally
			{
				typedArray.Recycle();
			}
		}

		public static bool ReadBooleanValue(Android.Content.Context context, IAttributeSet attrs, int[] groupId, int requiredAttributeId)
		{
			var typedArray = context.ObtainStyledAttributes(attrs, groupId);

			try
			{
				var numStyles = typedArray.IndexCount;
				for (var i = 0; i < numStyles; ++i)
				{
					var attributeId = typedArray.GetIndex(i);
					if (attributeId == requiredAttributeId)
					{
						return typedArray.GetBoolean(i, false);
					}
				}

				return false;
			}
			finally
			{
				typedArray.Recycle();
			}
		}
	}

}
