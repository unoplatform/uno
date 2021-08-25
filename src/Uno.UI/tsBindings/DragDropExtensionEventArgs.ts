/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class DragDropExtensionEventArgs
{
	/* Pack=8 */
	public eventName : string;
	public timestamp : number;
	public x : number;
	public y : number;
	public buttons : number;
	public shift : boolean;
	public ctrl : boolean;
	public alt : boolean;
	public allowedOperations : string;
	public acceptedOperation : string;
	public dataItems : string;
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
			ret.timestamp = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.x = Number(Module.getValue(pData + 16, "double"));
		}
		
		{
			ret.y = Number(Module.getValue(pData + 24, "double"));
		}
		
		{
			ret.buttons = Number(Module.getValue(pData + 32, "i32"));
		}
		
		{
			ret.shift = Boolean(Module.getValue(pData + 40, "i32"));
		}
		
		{
			ret.ctrl = Boolean(Module.getValue(pData + 48, "i32"));
		}
		
		{
			ret.alt = Boolean(Module.getValue(pData + 56, "i32"));
		}
		
		{
			const ptr = Module.getValue(pData + 64, "*");
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
			const ptr = Module.getValue(pData + 72, "*");
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
			const ptr = Module.getValue(pData + 80, "*");
			if(ptr !== 0)
			{
				ret.dataItems = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.dataItems = null;
			}
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
		Module.setValue(pData + 8, this.timestamp, "double");
		Module.setValue(pData + 16, this.x, "double");
		Module.setValue(pData + 24, this.y, "double");
		Module.setValue(pData + 32, this.buttons, "i32");
		Module.setValue(pData + 40, this.shift, "i32");
		Module.setValue(pData + 48, this.ctrl, "i32");
		Module.setValue(pData + 56, this.alt, "i32");
		
		{
			const stringLength = lengthBytesUTF8(this.allowedOperations);
			const pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.allowedOperations, pString, stringLength + 1);
			Module.setValue(pData + 64, pString, "*");
		}
		
		{
			const stringLength = lengthBytesUTF8(this.acceptedOperation);
			const pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.acceptedOperation, pString, stringLength + 1);
			Module.setValue(pData + 72, pString, "*");
		}
		
		{
			const stringLength = lengthBytesUTF8(this.dataItems);
			const pString = Module._malloc(stringLength + 1);
			stringToUTF8(this.dataItems, pString, stringLength + 1);
			Module.setValue(pData + 80, pString, "*");
		}
	}
}
