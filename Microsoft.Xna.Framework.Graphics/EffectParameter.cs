using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectParameter
{
	internal Texture texture;

	internal IntPtr values;

	internal uint valuesSizeBytes;

	public string Name { get; private set; }

	public string Semantic { get; private set; }

	public int RowCount { get; private set; }

	public int ColumnCount { get; private set; }

	public EffectParameterClass ParameterClass { get; private set; }

	public EffectParameterType ParameterType { get; private set; }

	public EffectParameterCollection Elements { get; private set; }

	public EffectParameterCollection StructureMembers { get; private set; }

	public EffectAnnotationCollection Annotations { get; private set; }

	internal EffectParameter(string name, string semantic, int rowCount, int columnCount, int elementCount, EffectParameterClass parameterClass, EffectParameterType parameterType, EffectParameterCollection structureMembers, EffectAnnotationCollection annotations, IntPtr data, uint dataSizeBytes)
	{
		if (data == IntPtr.Zero)
		{
			throw new ArgumentNullException("data");
		}
		Name = name;
		Semantic = semantic ?? string.Empty;
		RowCount = rowCount;
		ColumnCount = columnCount;
		if (elementCount > 0)
		{
			int num = 0;
			List<EffectParameter> list = new List<EffectParameter>(elementCount);
			for (int i = 0; i < elementCount; i++)
			{
				EffectParameterCollection structureMembers2 = null;
				if (structureMembers != null)
				{
					List<EffectParameter> list2 = new List<EffectParameter>();
					for (int j = 0; j < structureMembers.Count; j++)
					{
						int num2 = 0;
						if (structureMembers[j].Elements != null)
						{
							num2 = structureMembers[j].Elements.Count;
						}
						int num3 = structureMembers[j].RowCount * 4;
						if (num2 > 0)
						{
							num3 *= num2;
						}
						list2.Add(new EffectParameter(structureMembers[j].Name, structureMembers[j].Semantic, structureMembers[j].RowCount, structureMembers[j].ColumnCount, num2, structureMembers[j].ParameterClass, structureMembers[j].ParameterType, null, structureMembers[j].Annotations, new IntPtr(data.ToInt64() + num), (uint)(num3 * 4)));
						num += num3 * 4;
					}
					structureMembers2 = new EffectParameterCollection(list2);
				}
				list.Add(new EffectParameter(null, null, rowCount, columnCount, 0, ParameterClass, parameterType, structureMembers2, null, new IntPtr(data.ToInt64() + i * rowCount * 16), 0u));
			}
			Elements = new EffectParameterCollection(list);
		}
		ParameterClass = parameterClass;
		ParameterType = parameterType;
		StructureMembers = structureMembers;
		Annotations = annotations;
		values = data;
		valuesSizeBytes = dataSizeBytes;
	}

	public unsafe bool GetValueBoolean()
	{
		int* ptr = (int*)(void*)values;
		return *ptr != 0;
	}

	public unsafe bool[] GetValueBooleanArray(int count)
	{
		bool[] array = new bool[count];
		int* ptr = (int*)(void*)values;
		int num = 0;
		while (num < array.Length)
		{
			int num2 = 0;
			while (num2 < ColumnCount)
			{
				array[num] = ptr[num2] != 0;
				num2++;
				num++;
			}
			ptr += 4;
		}
		return array;
	}

	public unsafe int GetValueInt32()
	{
		int* ptr = (int*)(void*)values;
		return *ptr;
	}

	public int[] GetValueInt32Array(int count)
	{
		int[] array = new int[count];
		int num = 0;
		int num2 = 0;
		while (num < array.Length)
		{
			Marshal.Copy(values + num2, array, num, ColumnCount);
			num += ColumnCount;
			num2 += 16;
		}
		return array;
	}

	public unsafe Matrix GetValueMatrixTranspose()
	{
		float* ptr = (float*)(void*)values;
		return new Matrix(*ptr, ptr[1], ptr[2], ptr[3], ptr[4], ptr[5], ptr[6], ptr[7], ptr[8], ptr[9], ptr[10], ptr[11], ptr[12], ptr[13], ptr[14], ptr[15]);
	}

	public unsafe Matrix[] GetValueMatrixTransposeArray(int count)
	{
		Matrix[] array = new Matrix[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Matrix(*ptr, ptr[1], ptr[2], ptr[3], ptr[4], ptr[5], ptr[6], ptr[7], ptr[8], ptr[9], ptr[10], ptr[11], ptr[12], ptr[13], ptr[14], ptr[15]);
			num++;
			ptr += 16;
		}
		return array;
	}

	public unsafe Matrix GetValueMatrix()
	{
		float* ptr = (float*)(void*)values;
		return new Matrix(*ptr, ptr[4], ptr[8], ptr[12], ptr[1], ptr[5], ptr[9], ptr[13], ptr[2], ptr[6], ptr[10], ptr[14], ptr[3], ptr[7], ptr[11], ptr[15]);
	}

	public unsafe Matrix[] GetValueMatrixArray(int count)
	{
		Matrix[] array = new Matrix[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Matrix(*ptr, ptr[4], ptr[8], ptr[12], ptr[1], ptr[5], ptr[9], ptr[13], ptr[2], ptr[6], ptr[10], ptr[14], ptr[3], ptr[7], ptr[11], ptr[15]);
			num++;
			ptr += 16;
		}
		return array;
	}

	public unsafe Quaternion GetValueQuaternion()
	{
		float* ptr = (float*)(void*)values;
		return new Quaternion(*ptr, ptr[1], ptr[2], ptr[3]);
	}

	public unsafe Quaternion[] GetValueQuaternionArray(int count)
	{
		Quaternion[] array = new Quaternion[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Quaternion(*ptr, ptr[1], ptr[2], ptr[3]);
			num++;
			ptr += 4;
		}
		return array;
	}

	public unsafe float GetValueSingle()
	{
		float* ptr = (float*)(void*)values;
		return *ptr;
	}

	public float[] GetValueSingleArray(int count)
	{
		float[] array = new float[count];
		int num = 0;
		int num2 = 0;
		while (num < array.Length)
		{
			Marshal.Copy(values + num2, array, num, ColumnCount);
			num += ColumnCount;
			num2 += 16;
		}
		return array;
	}

	public string GetValueString()
	{
		throw new NotImplementedException("effect->objects[?]");
	}

	public Texture2D GetValueTexture2D()
	{
		return (Texture2D)texture;
	}

	public Texture3D GetValueTexture3D()
	{
		return (Texture3D)texture;
	}

	public TextureCube GetValueTextureCube()
	{
		return (TextureCube)texture;
	}

	public unsafe Vector2 GetValueVector2()
	{
		float* ptr = (float*)(void*)values;
		return new Vector2(*ptr, ptr[1]);
	}

	public unsafe Vector2[] GetValueVector2Array(int count)
	{
		Vector2[] array = new Vector2[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Vector2(*ptr, ptr[1]);
			num++;
			ptr += 4;
		}
		return array;
	}

	public unsafe Vector3 GetValueVector3()
	{
		float* ptr = (float*)(void*)values;
		return new Vector3(*ptr, ptr[1], ptr[2]);
	}

	public unsafe Vector3[] GetValueVector3Array(int count)
	{
		Vector3[] array = new Vector3[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Vector3(*ptr, ptr[1], ptr[2]);
			num++;
			ptr += 4;
		}
		return array;
	}

	public unsafe Vector4 GetValueVector4()
	{
		float* ptr = (float*)(void*)values;
		return new Vector4(*ptr, ptr[1], ptr[2], ptr[3]);
	}

	public unsafe Vector4[] GetValueVector4Array(int count)
	{
		Vector4[] array = new Vector4[count];
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < count)
		{
			array[num] = new Vector4(*ptr, ptr[1], ptr[2], ptr[3]);
			num++;
			ptr += 4;
		}
		return array;
	}

	public unsafe void SetValue(bool value)
	{
		int* ptr = (int*)(void*)values;
		*ptr = (value ? 1 : 0);
	}

	public unsafe void SetValue(bool[] value)
	{
		int* ptr = (int*)(void*)values;
		int num = 0;
		while (num < value.Length)
		{
			int num2 = 0;
			while (num2 < ColumnCount)
			{
				ptr[num2] = (value[num] ? 1 : 0);
				num2++;
				num++;
			}
			ptr += 4;
		}
	}

	public unsafe void SetValue(int value)
	{
		if (ParameterType == EffectParameterType.Single)
		{
			float* ptr = (float*)(void*)values;
			*ptr = value;
		}
		else
		{
			int* ptr2 = (int*)(void*)values;
			*ptr2 = value;
		}
	}

	public void SetValue(int[] value)
	{
		int num = 0;
		int num2 = 0;
		while (num < value.Length)
		{
			Marshal.Copy(value, num, values + num2, ColumnCount);
			num += ColumnCount;
			num2 += 16;
		}
	}

	public unsafe void SetValueTranspose(Matrix value)
	{
		float* ptr = (float*)(void*)values;
		if (ColumnCount == 4 && RowCount == 4)
		{
			*ptr = value.M11;
			ptr[1] = value.M12;
			ptr[2] = value.M13;
			ptr[3] = value.M14;
			ptr[4] = value.M21;
			ptr[5] = value.M22;
			ptr[6] = value.M23;
			ptr[7] = value.M24;
			ptr[8] = value.M31;
			ptr[9] = value.M32;
			ptr[10] = value.M33;
			ptr[11] = value.M34;
			ptr[12] = value.M41;
			ptr[13] = value.M42;
			ptr[14] = value.M43;
			ptr[15] = value.M44;
		}
		else if (ColumnCount == 3 && RowCount == 3)
		{
			*ptr = value.M11;
			ptr[1] = value.M12;
			ptr[2] = value.M13;
			ptr[4] = value.M21;
			ptr[5] = value.M22;
			ptr[6] = value.M23;
			ptr[8] = value.M31;
			ptr[9] = value.M32;
			ptr[10] = value.M33;
		}
		else if (ColumnCount == 4 && RowCount == 3)
		{
			*ptr = value.M11;
			ptr[1] = value.M12;
			ptr[2] = value.M13;
			ptr[4] = value.M21;
			ptr[5] = value.M22;
			ptr[6] = value.M23;
			ptr[8] = value.M31;
			ptr[9] = value.M32;
			ptr[10] = value.M33;
			ptr[12] = value.M41;
			ptr[13] = value.M42;
			ptr[14] = value.M43;
		}
		else if (ColumnCount == 3 && RowCount == 4)
		{
			*ptr = value.M11;
			ptr[1] = value.M12;
			ptr[2] = value.M13;
			ptr[3] = value.M14;
			ptr[4] = value.M21;
			ptr[5] = value.M22;
			ptr[6] = value.M23;
			ptr[7] = value.M24;
			ptr[8] = value.M31;
			ptr[9] = value.M32;
			ptr[10] = value.M33;
			ptr[11] = value.M34;
		}
		else
		{
			if (ColumnCount != 2 || RowCount != 2)
			{
				throw new NotImplementedException("Matrix Size: " + RowCount + " " + ColumnCount);
			}
			*ptr = value.M11;
			ptr[1] = value.M12;
			ptr[4] = value.M21;
			ptr[5] = value.M22;
		}
	}

	public unsafe void SetValueTranspose(Matrix[] value)
	{
		float* ptr = (float*)(void*)values;
		if (ColumnCount == 4 && RowCount == 4)
		{
			int num = 0;
			while (num < value.Length)
			{
				*ptr = value[num].M11;
				ptr[1] = value[num].M12;
				ptr[2] = value[num].M13;
				ptr[3] = value[num].M14;
				ptr[4] = value[num].M21;
				ptr[5] = value[num].M22;
				ptr[6] = value[num].M23;
				ptr[7] = value[num].M24;
				ptr[8] = value[num].M31;
				ptr[9] = value[num].M32;
				ptr[10] = value[num].M33;
				ptr[11] = value[num].M34;
				ptr[12] = value[num].M41;
				ptr[13] = value[num].M42;
				ptr[14] = value[num].M43;
				ptr[15] = value[num].M44;
				num++;
				ptr += 16;
			}
		}
		else if (ColumnCount == 3 && RowCount == 3)
		{
			int num2 = 0;
			while (num2 < value.Length)
			{
				*ptr = value[num2].M11;
				ptr[1] = value[num2].M12;
				ptr[2] = value[num2].M13;
				ptr[4] = value[num2].M21;
				ptr[5] = value[num2].M22;
				ptr[6] = value[num2].M23;
				ptr[8] = value[num2].M31;
				ptr[9] = value[num2].M32;
				ptr[10] = value[num2].M33;
				num2++;
				ptr += 12;
			}
		}
		else if (ColumnCount == 4 && RowCount == 3)
		{
			int num3 = 0;
			while (num3 < value.Length)
			{
				*ptr = value[num3].M11;
				ptr[1] = value[num3].M12;
				ptr[2] = value[num3].M13;
				ptr[4] = value[num3].M21;
				ptr[5] = value[num3].M22;
				ptr[6] = value[num3].M23;
				ptr[8] = value[num3].M31;
				ptr[9] = value[num3].M32;
				ptr[10] = value[num3].M33;
				ptr[12] = value[num3].M41;
				ptr[13] = value[num3].M42;
				ptr[14] = value[num3].M43;
				num3++;
				ptr += 16;
			}
		}
		else if (ColumnCount == 3 && RowCount == 4)
		{
			int num4 = 0;
			while (num4 < value.Length)
			{
				*ptr = value[num4].M11;
				ptr[1] = value[num4].M12;
				ptr[2] = value[num4].M13;
				ptr[3] = value[num4].M14;
				ptr[4] = value[num4].M21;
				ptr[5] = value[num4].M22;
				ptr[6] = value[num4].M23;
				ptr[7] = value[num4].M24;
				ptr[8] = value[num4].M31;
				ptr[9] = value[num4].M32;
				ptr[10] = value[num4].M33;
				ptr[11] = value[num4].M34;
				num4++;
				ptr += 12;
			}
		}
		else
		{
			if (ColumnCount != 2 || RowCount != 2)
			{
				throw new NotImplementedException("Matrix Size: " + RowCount + " " + ColumnCount);
			}
			int num5 = 0;
			while (num5 < value.Length)
			{
				*ptr = value[num5].M11;
				ptr[1] = value[num5].M12;
				ptr[4] = value[num5].M21;
				ptr[5] = value[num5].M22;
				num5++;
				ptr += 8;
			}
		}
	}

	public unsafe void SetValue(Matrix value)
	{
		float* ptr = (float*)(void*)values;
		if (ColumnCount == 4 && RowCount == 4)
		{
			*ptr = value.M11;
			ptr[1] = value.M21;
			ptr[2] = value.M31;
			ptr[3] = value.M41;
			ptr[4] = value.M12;
			ptr[5] = value.M22;
			ptr[6] = value.M32;
			ptr[7] = value.M42;
			ptr[8] = value.M13;
			ptr[9] = value.M23;
			ptr[10] = value.M33;
			ptr[11] = value.M43;
			ptr[12] = value.M14;
			ptr[13] = value.M24;
			ptr[14] = value.M34;
			ptr[15] = value.M44;
		}
		else if (ColumnCount == 3 && RowCount == 3)
		{
			*ptr = value.M11;
			ptr[1] = value.M21;
			ptr[2] = value.M31;
			ptr[4] = value.M12;
			ptr[5] = value.M22;
			ptr[6] = value.M32;
			ptr[8] = value.M13;
			ptr[9] = value.M23;
			ptr[10] = value.M33;
		}
		else if (ColumnCount == 4 && RowCount == 3)
		{
			*ptr = value.M11;
			ptr[1] = value.M21;
			ptr[2] = value.M31;
			ptr[3] = value.M41;
			ptr[4] = value.M12;
			ptr[5] = value.M22;
			ptr[6] = value.M32;
			ptr[7] = value.M42;
			ptr[8] = value.M13;
			ptr[9] = value.M23;
			ptr[10] = value.M33;
			ptr[11] = value.M43;
		}
		else if (ColumnCount == 3 && RowCount == 4)
		{
			*ptr = value.M11;
			ptr[1] = value.M21;
			ptr[2] = value.M31;
			ptr[4] = value.M12;
			ptr[5] = value.M22;
			ptr[6] = value.M32;
			ptr[8] = value.M13;
			ptr[9] = value.M23;
			ptr[10] = value.M33;
			ptr[12] = value.M14;
			ptr[13] = value.M24;
			ptr[14] = value.M34;
		}
		else
		{
			if (ColumnCount != 2 || RowCount != 2)
			{
				throw new NotImplementedException("Matrix Size: " + RowCount + " " + ColumnCount);
			}
			*ptr = value.M11;
			ptr[1] = value.M21;
			ptr[4] = value.M12;
			ptr[5] = value.M22;
		}
	}

	public unsafe void SetValue(Matrix[] value)
	{
		float* ptr = (float*)(void*)values;
		if (ColumnCount == 4 && RowCount == 4)
		{
			int num = 0;
			while (num < value.Length)
			{
				*ptr = value[num].M11;
				ptr[1] = value[num].M21;
				ptr[2] = value[num].M31;
				ptr[3] = value[num].M41;
				ptr[4] = value[num].M12;
				ptr[5] = value[num].M22;
				ptr[6] = value[num].M32;
				ptr[7] = value[num].M42;
				ptr[8] = value[num].M13;
				ptr[9] = value[num].M23;
				ptr[10] = value[num].M33;
				ptr[11] = value[num].M43;
				ptr[12] = value[num].M14;
				ptr[13] = value[num].M24;
				ptr[14] = value[num].M34;
				ptr[15] = value[num].M44;
				num++;
				ptr += 16;
			}
		}
		else if (ColumnCount == 3 && RowCount == 3)
		{
			int num2 = 0;
			while (num2 < value.Length)
			{
				*ptr = value[num2].M11;
				ptr[1] = value[num2].M21;
				ptr[2] = value[num2].M31;
				ptr[4] = value[num2].M12;
				ptr[5] = value[num2].M22;
				ptr[6] = value[num2].M32;
				ptr[8] = value[num2].M13;
				ptr[9] = value[num2].M23;
				ptr[10] = value[num2].M33;
				num2++;
				ptr += 12;
			}
		}
		else if (ColumnCount == 4 && RowCount == 3)
		{
			int num3 = 0;
			while (num3 < value.Length)
			{
				*ptr = value[num3].M11;
				ptr[1] = value[num3].M21;
				ptr[2] = value[num3].M31;
				ptr[3] = value[num3].M41;
				ptr[4] = value[num3].M12;
				ptr[5] = value[num3].M22;
				ptr[6] = value[num3].M32;
				ptr[7] = value[num3].M42;
				ptr[8] = value[num3].M13;
				ptr[9] = value[num3].M23;
				ptr[10] = value[num3].M33;
				ptr[11] = value[num3].M43;
				num3++;
				ptr += 12;
			}
		}
		else if (ColumnCount == 3 && RowCount == 4)
		{
			int num4 = 0;
			while (num4 < value.Length)
			{
				*ptr = value[num4].M11;
				ptr[1] = value[num4].M21;
				ptr[2] = value[num4].M31;
				ptr[4] = value[num4].M12;
				ptr[5] = value[num4].M22;
				ptr[6] = value[num4].M32;
				ptr[8] = value[num4].M13;
				ptr[9] = value[num4].M23;
				ptr[10] = value[num4].M33;
				ptr[12] = value[num4].M14;
				ptr[13] = value[num4].M24;
				ptr[14] = value[num4].M34;
				num4++;
				ptr += 16;
			}
		}
		else
		{
			if (ColumnCount != 2 || RowCount != 2)
			{
				throw new NotImplementedException("Matrix Size: " + RowCount + " " + ColumnCount);
			}
			int num5 = 0;
			while (num5 < value.Length)
			{
				*ptr = value[num5].M11;
				ptr[1] = value[num5].M21;
				ptr[4] = value[num5].M12;
				ptr[5] = value[num5].M22;
				num5++;
				ptr += 8;
			}
		}
	}

	public unsafe void SetValue(Quaternion value)
	{
		float* ptr = (float*)(void*)values;
		*ptr = value.X;
		ptr[1] = value.Y;
		ptr[2] = value.Z;
		ptr[3] = value.W;
	}

	public unsafe void SetValue(Quaternion[] value)
	{
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < value.Length)
		{
			*ptr = value[num].X;
			ptr[1] = value[num].Y;
			ptr[2] = value[num].Z;
			ptr[3] = value[num].W;
			num++;
			ptr += 4;
		}
	}

	public unsafe void SetValue(float value)
	{
		float* ptr = (float*)(void*)values;
		*ptr = value;
	}

	public void SetValue(float[] value)
	{
		int num = 0;
		int num2 = 0;
		while (num < value.Length)
		{
			Marshal.Copy(value, num, values + num2, ColumnCount);
			num += ColumnCount;
			num2 += 16;
		}
	}

	public void SetValue(string value)
	{
		throw new NotImplementedException("effect->objects[?]");
	}

	public void SetValue(Texture value)
	{
		texture = value;
	}

	public unsafe void SetValue(Vector2 value)
	{
		float* ptr = (float*)(void*)values;
		*ptr = value.X;
		ptr[1] = value.Y;
	}

	public unsafe void SetValue(Vector2[] value)
	{
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < value.Length)
		{
			*ptr = value[num].X;
			ptr[1] = value[num].Y;
			num++;
			ptr += 4;
		}
	}

	public unsafe void SetValue(Vector3 value)
	{
		float* ptr = (float*)(void*)values;
		*ptr = value.X;
		ptr[1] = value.Y;
		ptr[2] = value.Z;
	}

	public unsafe void SetValue(Vector3[] value)
	{
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < value.Length)
		{
			*ptr = value[num].X;
			ptr[1] = value[num].Y;
			ptr[2] = value[num].Z;
			num++;
			ptr += 4;
		}
	}

	public unsafe void SetValue(Vector4 value)
	{
		float* ptr = (float*)(void*)values;
		*ptr = value.X;
		ptr[1] = value.Y;
		ptr[2] = value.Z;
		ptr[3] = value.W;
	}

	public unsafe void SetValue(Vector4[] value)
	{
		float* ptr = (float*)(void*)values;
		int num = 0;
		while (num < value.Length)
		{
			*ptr = value[num].X;
			ptr[1] = value[num].Y;
			ptr[2] = value[num].Z;
			ptr[3] = value[num].W;
			num++;
			ptr += 4;
		}
	}
}
