/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_SingleStringParams
{
	/* Pack=1 */
	MyString : string;
	public static unmarshal(pData:number) : When_SingleStringParams
	{
		let ret = new When_SingleStringParams();
		
		{
			var ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.MyString = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.MyString = null;
			}
		}
		return ret;
	}
}
