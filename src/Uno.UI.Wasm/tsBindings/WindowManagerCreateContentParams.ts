/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerCreateContentParams
{
	/* Pack=4 */
	HtmlId : number;
	TagName : string;
	Handle : number;
	Type : string;
	IsSvg : boolean;
	IsFrameworkElement : boolean;
	IsFocusable : boolean;
	Classes_Length : number;
	Classes : Array<string>;
	public static unmarshal(pData:number) : WindowManagerCreateContentParams
	{
		let ret = new WindowManagerCreateContentParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.TagName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.TagName = null;
			}
		}
		
		{
			ret.Handle = Number(Module.getValue(pData + 8, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 12, "*");
			if(ptr !== 0)
			{
				ret.Type = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Type = null;
			}
		}
		
		{
			ret.IsSvg = Boolean(Module.getValue(pData + 16, "i32"));
		}
		
		{
			ret.IsFrameworkElement = Boolean(Module.getValue(pData + 20, "i32"));
		}
		
		{
			ret.IsFocusable = Boolean(Module.getValue(pData + 24, "i32"));
		}
		
		{
			ret.Classes_Length = Number(Module.getValue(pData + 28, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 32, "*");
			if(pArray !== 0)
			{
				ret.Classes = new Array<string>();
				for(var i=0; i<ret.Classes_Length; i++)
				{
					var value = Module.getValue(pArray + i * 4, "*");
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
