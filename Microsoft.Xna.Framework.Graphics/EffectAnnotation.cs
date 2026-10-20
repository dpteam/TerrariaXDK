using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectAnnotation
{
	private IntPtr values;

	public string Name { get; private set; }

	public string Semantic { get; private set; }

	public int RowCount { get; private set; }

	public int ColumnCount { get; private set; }

	public EffectParameterClass ParameterClass { get; private set; }

	public EffectParameterType ParameterType { get; private set; }

	internal EffectAnnotation(string name, string semantic, int rowCount, int columnCount, EffectParameterClass parameterClass, EffectParameterType parameterType, IntPtr data)
	{
		Name = name;
		Semantic = semantic ?? string.Empty;
		RowCount = rowCount;
		ColumnCount = columnCount;
		ParameterClass = parameterClass;
		ParameterType = parameterType;
		values = data;
	}

	public unsafe bool GetValueBoolean()
	{
		int* ptr = (int*)(void*)values;
		return *ptr != 0;
	}

	public unsafe int GetValueInt32()
	{
		int* ptr = (int*)(void*)values;
		return *ptr;
	}

	public unsafe Matrix GetValueMatrix()
	{
		float* ptr = (float*)(void*)values;
		return new Matrix(*ptr, ptr[4], ptr[8], ptr[12], ptr[1], ptr[5], ptr[9], ptr[13], ptr[2], ptr[6], ptr[10], ptr[14], ptr[3], ptr[7], ptr[11], ptr[15]);
	}

	public unsafe float GetValueSingle()
	{
		float* ptr = (float*)(void*)values;
		return *ptr;
	}

	public string GetValueString()
	{
		throw new NotImplementedException("effect->objects[?]");
	}

	public unsafe Vector2 GetValueVector2()
	{
		float* ptr = (float*)(void*)values;
		return new Vector2(*ptr, ptr[1]);
	}

	public unsafe Vector3 GetValueVector3()
	{
		float* ptr = (float*)(void*)values;
		return new Vector3(*ptr, ptr[1], ptr[2]);
	}

	public unsafe Vector4 GetValueVector4()
	{
		float* ptr = (float*)(void*)values;
		return new Vector4(*ptr, ptr[1], ptr[2], ptr[3]);
	}
}
