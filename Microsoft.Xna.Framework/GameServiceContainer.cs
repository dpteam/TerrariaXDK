using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework;

public class GameServiceContainer : IServiceProvider
{
	private Dictionary<Type, object> services;

	public GameServiceContainer()
	{
		services = new Dictionary<Type, object>();
	}

	public void AddService(Type type, object provider)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (!type.IsAssignableFrom(provider.GetType()))
		{
			throw new ArgumentException("The provider does not match the specified service type!");
		}
		services.Add(type, provider);
	}

	public object GetService(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (services.TryGetValue(type, out var value))
		{
			return value;
		}
		return null;
	}

	public void RemoveService(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		services.Remove(type);
	}
}
