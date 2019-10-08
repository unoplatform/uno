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

/*

* State transition

Unlike XmlWriter, XAML nodes are not immediately writable because object
output has to be delayed to be determined whether it should write
an attribute or an element.

** NamespaceDeclarations

NamespaceDeclaration does not immediately participate in the state transition
but some write methods reject stored namespaces (e.g. WriteEndObject cannot
handle them). In such cases, they throw InvalidOperationException, while the
writer throws XamlXmlWriterException for usual state transition.

Though they still seems to affect some outputs. If a member with simple
value is written after a namespace, then it becomes an element, not attribute.

** state transition

states are: Initial, ObjectStarted, MemberStarted, ValueWritten, MemberDone, End

Initial + StartObject -> ObjectStarted : push(xt)
ObjectStarted + StartMember -> MemberStarted : push(xm)
ObjectStarted + EndObject -> ObjectWritten or End : pop()
MemberStarted + StartObject -> ObjectStarted : push(xt)
MemberStarted + Value -> ValueWritten
MemberStarted + GetObject -> MemberDone : pop()
ObjectWritten + StartObject -> ObjectStarted : push(x)
ObjectWritten + Value -> ValueWritten : pop()
ObjectWritten + EndMember -> MemberDone : pop()
ValueWritten + StartObject -> invalid - or - ObjectStarted : push(x)
ValueWritten + Value -> invalid - or - ValueWritten
ValueWritten + EndMember -> MemberDone : pop()
MemberDone + EndObject -> ObjectWritten or End : pop() // xt
MemberDone + StartMember -> MemberStarted : push(xm)

(in XamlObjectWriter, Value must be followed by EndMember.)

*/

namespace Uno.Xaml
{
	internal class XamlWriterStateManager<TError,TNsError> : XamlWriterStateManager
		where TError : Exception
		where TNsError : Exception
	{
		public XamlWriterStateManager (bool isXmlWriter)
			: base (isXmlWriter)
		{
		}

		public override Exception CreateError (string msg)
		{
			return (Exception) Activator.CreateInstance (typeof (TError), new object [] {msg});
		}

		public override Exception CreateNamespaceError (string msg)
		{
			return (Exception) Activator.CreateInstance (typeof (TNsError), new object [] {msg});
		}
	}

	internal enum XamlWriteState
	{
		Initial,
		ObjectStarted,
		MemberStarted,
		ObjectWritten,
		ValueWritten,
		MemberDone,
		End
	}

	internal abstract class XamlWriterStateManager
	{
		public XamlWriterStateManager (bool isXmlWriter)
		{
			_allowNsAtValue = isXmlWriter;
			_allowObjectAfterValue = isXmlWriter;
			_allowParallelValues = !isXmlWriter;
			_allowEmptyMember = !isXmlWriter;
			_allowMultipleResults = !isXmlWriter;
		}

		// configuration
		private readonly bool _allowNsAtValue;
		private readonly bool _allowObjectAfterValue;
		private readonly bool _allowParallelValues;
		private readonly bool _allowEmptyMember;
		private readonly bool _allowMultipleResults;

		// state
		private bool _nsPushed;

		public XamlWriteState State
		{
			get;
			private set;
		} = XamlWriteState.Initial;

		// FIXME: actually this property is a hack. It should preserve stacked flag values for each nested member in current tree state.
		public bool AcceptMultipleValues
		{
			get;
			set;
		}

		public void OnClosingItem ()
		{
			// somewhat hacky state change to not reject StartMember->EndMember.
			if (State == XamlWriteState.MemberStarted)
			{
				State = XamlWriteState.ValueWritten;
			}
		}

		public void EndMember ()
		{
			RejectNamespaces (XamlNodeType.EndMember);
			CheckState (XamlNodeType.EndMember);
			State = XamlWriteState.MemberDone;
		}

		public void EndObject (bool hasMoreNodes)
		{
			RejectNamespaces (XamlNodeType.EndObject);
			CheckState (XamlNodeType.EndObject);
			State = hasMoreNodes ? XamlWriteState.ObjectWritten : _allowMultipleResults ? XamlWriteState.Initial : XamlWriteState.End;
		}

		public void GetObject ()
		{
			CheckState (XamlNodeType.GetObject);
			RejectNamespaces (XamlNodeType.GetObject);
			State = XamlWriteState.MemberDone;
		}

		public void StartMember ()
		{
			CheckState (XamlNodeType.StartMember);
			State = XamlWriteState.MemberStarted;
			_nsPushed = false;
		}

		public void StartObject ()
		{
			CheckState (XamlNodeType.StartObject);
			State = XamlWriteState.ObjectStarted;
			_nsPushed = false;
		}

		public void Value ()
		{
			CheckState (XamlNodeType.Value);
			RejectNamespaces (XamlNodeType.Value);
			State = XamlWriteState.ValueWritten;
		}

		public void Namespace ()
		{
			if (!_allowNsAtValue && (State == XamlWriteState.ValueWritten || State == XamlWriteState.ObjectStarted))
			{
				throw CreateError (string.Format ("Namespace declarations cannot be written at {0} state", State));
			}

			_nsPushed = true;
		}

		public void NamespaceCleanedUp ()
		{
			_nsPushed = false;
		}

		private void CheckState (XamlNodeType next)
		{
			switch (State) {
			case XamlWriteState.Initial:
				switch (next) {
				case XamlNodeType.StartObject:
					return;
				}
				break;
			case XamlWriteState.ObjectStarted:
				switch (next) {
				case XamlNodeType.StartMember:
				case XamlNodeType.EndObject:
					return;
				}
				break;
			case XamlWriteState.MemberStarted:
				switch (next) {
				case XamlNodeType.StartObject:
				case XamlNodeType.Value:
				case XamlNodeType.GetObject:
					return;
				case XamlNodeType.EndMember:
					if (_allowEmptyMember)
							{
								return;
							}

							break;
				}
				break;
			case XamlWriteState.ObjectWritten:
				switch (next) {
				case XamlNodeType.StartObject:
				case XamlNodeType.Value:
				case XamlNodeType.EndMember:
					return;
				}
				break;
			case XamlWriteState.ValueWritten:
				switch (next) {
				case XamlNodeType.Value:
					if (_allowParallelValues | AcceptMultipleValues)
							{
								return;
							}

							break;
				case XamlNodeType.StartObject:
					if (_allowObjectAfterValue)
							{
								return;
							}

							break;
				case XamlNodeType.EndMember:
					return;
				}
				break;
			case XamlWriteState.MemberDone:
				switch (next) {
				case XamlNodeType.StartMember:
				case XamlNodeType.EndObject:
					return;
				}
				break;
			}
			throw CreateError (string.Format ("{0} is not allowed at current state {1}", next, State));
		}

		private void RejectNamespaces (XamlNodeType next)
		{
			if (_nsPushed) {
				// strange, but on WriteEndMember it throws XamlXmlWriterException, while for other nodes it throws IOE.
				string msg = string.Format ("Namespace declarations cannot be written before {0}", next);
				if (next == XamlNodeType.EndMember)
				{
					throw CreateError (msg);
				}
				else
				{
					throw CreateNamespaceError (msg);
				}
			}
		}

		public abstract Exception CreateError (string msg);
		public abstract Exception CreateNamespaceError (string msg);
	}
}
