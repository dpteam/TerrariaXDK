using System;

namespace ObjCRuntime;

[AttributeUsage(AttributeTargets.Method)]
internal class MonoPInvokeCallbackAttribute : Attribute
{
	public MonoPInvokeCallbackAttribute(Type t)
	{
	}
}
