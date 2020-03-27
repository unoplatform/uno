/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterUIElementParams
{
	/* Pack=4 */
	public TypeName : string;
	public IsFrameworkElement : boolean;
	public Classes_Length : number;
	public Classes : Array<string>;
	public static unmarshal(pData:number) : WindowManagerRegisterUIElementParams
	{
		const ret = new WindowManagerRegisterUIElementParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.TypeName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.TypeName = null;
			}
		}
		
		{
			ret.IsFrameworkElement = Boolean(Module.getValue(pData + 4, "i32"));
		}
		
		{
			ret.Classes_Length = Number(Module.getValue(pData + 8, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 12, "*");
			if(pArray !== 0)
			{
				ret.Classes = new Array<string>();
				for(var i=0; i<ret.Classes_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Classes.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Classes.push(null);
					}
				}
			}
			else
			
			{
				ret.Classes = null;
			}
		}
		return ret;
	}
}
