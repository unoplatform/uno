/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	export class DragDropExtensionEventArgs
	{
		/* Pack=4 */
		public eventName : string;
		public allowedOperations : string;
		public acceptedOperation : string;
		public dataItems : string;
		public timestamp : number;
		public x : number;
		public y : number;
		public id : number;
		public buttons : number;
		public shift : boolean;
		public ctrl : boolean;
		public alt : boolean;
		public static unmarshal(pData:number) : DragDropExtensionEventArgs
		{
			const ret = new DragDropExtensionEventArgs();
			
			{
				const ptr = Module.getValue(pData + 0, "*");
				if(ptr !== 0)
				{
					ret.eventName = String(Module.UTF8ToString(ptr));
				}
				else
				
				{
					ret.eventName = null;
				}
			}
			
			{
				const ptr = Module.getValue(pData + 4, "*");
				if(ptr !== 0)
				{
					ret.allowedOperations = String(Module.UTF8ToString(ptr));
				}
				else
				
				{
					ret.allowedOperations = null;
				}
			}
			
			{
				const ptr = Module.getValue(pData + 8, "*");
				if(ptr !== 0)
				{
					ret.acceptedOperation = String(Module.UTF8ToString(ptr));
				}
				else
				
				{
					ret.acceptedOperation = null;
				}
			}
			
			{
				const ptr = Module.getValue(pData + 12, "*");
				if(ptr !== 0)
				{
					ret.dataItems = String(Module.UTF8ToString(ptr));
				}
				else
				
				{
					ret.dataItems = null;
				}
			}
			
			{
				ret.timestamp = Number(Module.getValue(pData + 16, "double"));
			}
			
			{
				ret.x = Number(Module.getValue(pData + 24, "double"));
			}
			
			{
				ret.y = Number(Module.getValue(pData + 32, "double"));
			}
			
			{
				ret.id = Number(Module.getValue(pData + 40, "i32"));
			}
			
			{
				ret.buttons = Number(Module.getValue(pData + 44, "i32"));
			}
			
			{
				ret.shift = Boolean(Module.getValue(pData + 48, "i32"));
			}
			
			{
				ret.ctrl = Boolean(Module.getValue(pData + 52, "i32"));
			}
			
			{
				ret.alt = Boolean(Module.getValue(pData + 56, "i32"));
			}
			return ret;
		}
		public marshal(pData:number)
		{
			
			{
				const stringLength = lengthBytesUTF8(this.eventName);
				const pString = Module._malloc(stringLength + 1);
				stringToUTF8(this.eventName, pString, stringLength + 1);
				Module.setValue(pData + 0, pString, "*");
			}
			
			{
				const stringLength = lengthBytesUTF8(this.allowedOperations);
				const pString = Module._malloc(stringLength + 1);
				stringToUTF8(this.allowedOperations, pString, stringLength + 1);
				Module.setValue(pData + 4, pString, "*");
			}
			
			{
				const stringLength = lengthBytesUTF8(this.acceptedOperation);
				const pString = Module._malloc(stringLength + 1);
				stringToUTF8(this.acceptedOperation, pString, stringLength + 1);
				Module.setValue(pData + 8, pString, "*");
			}
			
			{
				const stringLength = lengthBytesUTF8(this.dataItems);
				const pString = Module._malloc(stringLength + 1);
				stringToUTF8(this.dataItems, pString, stringLength + 1);
				Module.setValue(pData + 12, pString, "*");
			}
			Module.setValue(pData + 16, this.timestamp, "double");
			Module.setValue(pData + 24, this.x, "double");
			Module.setValue(pData + 32, this.y, "double");
			Module.setValue(pData + 40, this.id, "i32");
			Module.setValue(pData + 44, this.buttons, "i32");
			Module.setValue(pData + 48, this.shift, "i32");
			Module.setValue(pData + 52, this.ctrl, "i32");
			Module.setValue(pData + 56, this.alt, "i32");
		}
	}
}
