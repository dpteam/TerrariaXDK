using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Theorafile
{
	public enum SeekWhence
	{
		TF_SEEK_SET,
		TF_SEEK_CUR,
		TF_SEEK_END
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr read_func(IntPtr ptr, IntPtr size, IntPtr nmemb, IntPtr datasource);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int seek_func(IntPtr datasource, long offset, SeekWhence whence);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int close_func(IntPtr datasource);

	public enum th_pixel_fmt
	{
		TH_PF_420,
		TH_PF_RSVD,
		TH_PF_422,
		TH_PF_444,
		TH_PF_NFORMATS
	}

	public struct tf_callbacks
	{
		public read_func read_func;

		public seek_func seek_func;

		public close_func close_func;
	}

	private const string nativeLibName = "libtheorafile";

	private unsafe static byte* Utf8Encode(string str)
	{
		int num = str.Length * 4 + 1;
		byte* ptr = (byte*)(void*)Marshal.AllocHGlobal(num);
		fixed (char* chars = str)
		{
			Encoding.UTF8.GetBytes(chars, str.Length + 1, ptr, num);
		}
		return ptr;
	}

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl, EntryPoint = "tf_open_callbacks")]
	private static extern int INTERNAL_tf_open_callbacks(IntPtr datasource, IntPtr file, tf_callbacks io);

	public static int tf_open_callbacks(IntPtr datasource, out IntPtr file, tf_callbacks io)
	{
		file = AllocTheoraFile();
		return INTERNAL_tf_open_callbacks(datasource, file, io);
	}

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl, EntryPoint = "tf_fopen")]
	private unsafe static extern int INTERNAL_tf_fopen(byte* fname, IntPtr file);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl, EntryPoint = "tf_fopen")]
	private static extern int INTERNAL_tf_fopen([MarshalAs(UnmanagedType.LPStr)] string fname, IntPtr file);

	public unsafe static int tf_fopen(string fname, out IntPtr file)
	{
		file = AllocTheoraFile();
		int result;
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			result = INTERNAL_tf_fopen(fname, file);
		}
		else
		{
			byte* ptr = Utf8Encode(fname);
			result = INTERNAL_tf_fopen(ptr, file);
			Marshal.FreeHGlobal((IntPtr)ptr);
		}
		return result;
	}

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl, EntryPoint = "tf_close")]
	private static extern int INTERNAL_tf_close(IntPtr file);

	public static int tf_close(ref IntPtr file)
	{
		int result = INTERNAL_tf_close(file);
		Marshal.FreeHGlobal(file);
		file = IntPtr.Zero;
		return result;
	}

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_hasaudio(IntPtr file);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_hasvideo(IntPtr file);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_videoinfo(IntPtr file, out int width, out int height, out double fps, out th_pixel_fmt fmt);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_audioinfo(IntPtr file, out int channels, out int samplerate);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_setaudiotrack(IntPtr file, int track);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_setvideotrack(IntPtr file, int track);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_eos(IntPtr file);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_reset(IntPtr file);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_readvideo(IntPtr file, IntPtr buffer, int numframes);

	[DllImport("libtheorafile", CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_readaudio(IntPtr file, IntPtr buffer, int length);

	private static IntPtr AllocTheoraFile()
	{
		PlatformID platform = Environment.OSVersion.Platform;
		if (IntPtr.Size == 4)
		{
			return Marshal.AllocHGlobal(1160);
		}
		if (IntPtr.Size == 8)
		{
			if (1 == 0)
			{
			}
			IntPtr result = platform switch
			{
				PlatformID.Unix => Marshal.AllocHGlobal(1472), 
				PlatformID.Win32NT => Marshal.AllocHGlobal(1328), 
				_ => throw new NotSupportedException("Unhandled platform!"), 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		throw new NotSupportedException("Unhandled architecture!");
	}
}
