/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class StorageFolderMakePersistentParams
{
	/* Pack=4 */
	public Paths_Length : number;
	public Paths : Array<string>;
	public static unmarshal(pData:number) : StorageFolderMakePersistentParams
	{
		const ret = new StorageFolderMakePersistentParams();
		
		{
			ret.Paths_Length = Number(Module.getValue(pData + 0, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 4, "*");
			if(pArray !== 0)
			{
				ret.Paths = new Array<string>();
				for(var i=0; i<ret.Paths_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Paths.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Paths.push(null);
					}
				}
			}
			else
			
			{
				ret.Paths = null;
			}
		}
		return ret;
	}
}
