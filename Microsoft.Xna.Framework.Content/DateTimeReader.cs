using System;

namespace Microsoft.Xna.Framework.Content;

internal class DateTimeReader : ContentTypeReader<DateTime>
{
	internal DateTimeReader()
	{
	}

	protected internal override DateTime Read(ContentReader input, DateTime existingInstance)
	{
		ulong num = input.ReadUInt64();
		ulong num2 = 13835058055282163712uL;
		long ticks = (long)(num & ~num2);
		DateTimeKind kind = (DateTimeKind)((num >> 62) & 3);
		return new DateTime(ticks, kind);
	}
}
