//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;

namespace Uno.Xaml
{
	public class AttachableMemberIdentifier : IEquatable<AttachableMemberIdentifier>
	{
		public AttachableMemberIdentifier (Type declaringType, string memberName)
		{
			DeclaringType = declaringType;
			MemberName = memberName;
		}
		
		public Type DeclaringType { get; }
		public string MemberName { get; }
		
		public static bool operator == (AttachableMemberIdentifier left, AttachableMemberIdentifier right)
		{
			return left != null && (IsNull (left) ? IsNull (right) : left.Equals (right));
		}

		private static bool IsNull (AttachableMemberIdentifier a)
		{
			return ReferenceEquals (a, null);
		}

		public static bool operator != (AttachableMemberIdentifier left, AttachableMemberIdentifier right)
		{
			return left != null && (IsNull (left) ? !IsNull (right) : IsNull (right) || left.DeclaringType != right.DeclaringType || left.MemberName != right.MemberName);
		}
		
		public bool Equals (AttachableMemberIdentifier other)
		{
			return other != null && (!IsNull (other) && DeclaringType == other.DeclaringType && MemberName == other.MemberName);
		}

		public override bool Equals (object obj)
		{
			var a = obj as AttachableMemberIdentifier;
			return Equals (a);
		}

		public override int GetHashCode ()
		{
			return (DeclaringType != null ? DeclaringType.GetHashCode () : 0) << 5 + (MemberName != null ? MemberName.GetHashCode () : 0);
		}

		public override string ToString ()
		{
			return DeclaringType != null ? string.Concat (DeclaringType.FullName, ".", MemberName) : MemberName;
		}
	}
}
