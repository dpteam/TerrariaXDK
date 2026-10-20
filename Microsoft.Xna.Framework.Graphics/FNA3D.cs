using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using ObjCRuntime;
using SDL2;

namespace Microsoft.Xna.Framework.Graphics;

[SuppressUnmanagedCodeSecurity]
internal static class FNA3D
{
	public struct FNA3D_Viewport
	{
		public int x;

		public int y;

		public int w;

		public int h;

		public float minDepth;

		public float maxDepth;
	}

	public struct FNA3D_BlendState
	{
		public Blend colorSourceBlend;

		public Blend colorDestinationBlend;

		public BlendFunction colorBlendFunction;

		public Blend alphaSourceBlend;

		public Blend alphaDestinationBlend;

		public BlendFunction alphaBlendFunction;

		public ColorWriteChannels colorWriteEnable;

		public ColorWriteChannels colorWriteEnable1;

		public ColorWriteChannels colorWriteEnable2;

		public ColorWriteChannels colorWriteEnable3;

		public Color blendFactor;

		public int multiSampleMask;
	}

	public struct FNA3D_DepthStencilState
	{
		public byte depthBufferEnable;

		public byte depthBufferWriteEnable;

		public CompareFunction depthBufferFunction;

		public byte stencilEnable;

		public int stencilMask;

		public int stencilWriteMask;

		public byte twoSidedStencilMode;

		public StencilOperation stencilFail;

		public StencilOperation stencilDepthBufferFail;

		public StencilOperation stencilPass;

		public CompareFunction stencilFunction;

		public StencilOperation ccwStencilFail;

		public StencilOperation ccwStencilDepthBufferFail;

		public StencilOperation ccwStencilPass;

		public CompareFunction ccwStencilFunction;

		public int referenceStencil;
	}

	public struct FNA3D_RasterizerState
	{
		public FillMode fillMode;

		public CullMode cullMode;

		public float depthBias;

		public float slopeScaleDepthBias;

		public byte scissorTestEnable;

		public byte multiSampleAntiAlias;
	}

	public struct FNA3D_SamplerState
	{
		public TextureFilter filter;

		public TextureAddressMode addressU;

		public TextureAddressMode addressV;

		public TextureAddressMode addressW;

		public float mipMapLevelOfDetailBias;

		public int maxAnisotropy;

		public int maxMipLevel;
	}

	public struct FNA3D_VertexDeclaration
	{
		public int vertexStride;

		public int elementCount;

		public IntPtr elements;
	}

	public struct FNA3D_VertexBufferBinding
	{
		public IntPtr vertexBuffer;

		public FNA3D_VertexDeclaration vertexDeclaration;

		public int vertexOffset;

		public int instanceFrequency;
	}

	public struct FNA3D_RenderTargetBinding
	{
		public byte type;

		public int data1;

		public int data2;

		public int levelCount;

		public int multiSampleCount;

		public IntPtr texture;

		public IntPtr colorBuffer;
	}

	public struct FNA3D_PresentationParameters
	{
		public int backBufferWidth;

		public int backBufferHeight;

		public SurfaceFormat backBufferFormat;

		public int multiSampleCount;

		public IntPtr deviceWindowHandle;

		public byte isFullScreen;

		public DepthFormat depthStencilFormat;

		public PresentInterval presentationInterval;

		public DisplayOrientation displayOrientation;

		public RenderTargetUsage renderTargetUsage;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void FNA3D_LogFunc(IntPtr msg);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int FNA3D_Image_ReadFunc(IntPtr context, IntPtr data, int size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void FNA3D_Image_SkipFunc(IntPtr context, int n);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int FNA3D_Image_EOFFunc(IntPtr context);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void FNA3D_Image_WriteFunc(IntPtr context, IntPtr data, int size);

	private const string nativeLibName = "FNA3D";

	private static FNA3D_Image_ReadFunc readFunc = INTERNAL_Read;

	private static FNA3D_Image_SkipFunc skipFunc = INTERNAL_Skip;

	private static FNA3D_Image_EOFFunc eofFunc = INTERNAL_EOF;

	private static int readGlobal = 0;

	private static Dictionary<IntPtr, Stream> readStreams = new Dictionary<IntPtr, Stream>();

	private static FNA3D_Image_WriteFunc writeFunc = INTERNAL_Write;

	private static int writeGlobal = 0;

	private static Dictionary<IntPtr, Stream> writeStreams = new Dictionary<IntPtr, Stream>();

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FNA3D_LinkedVersion();

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_HookLogFunctions(FNA3D_LogFunc info, FNA3D_LogFunc warn, FNA3D_LogFunc error);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FNA3D_PrepareWindowAttributes();

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetDrawableSize(IntPtr window, out int w, out int h);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_CreateDevice(ref FNA3D_PresentationParameters presentationParameters, byte debugMode);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_DestroyDevice(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SwapBuffers(IntPtr device, ref Rectangle sourceRectangle, ref Rectangle destinationRectangle, IntPtr overrideWindowHandle);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SwapBuffers(IntPtr device, IntPtr sourceRectangle, IntPtr destinationRectangle, IntPtr overrideWindowHandle);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SwapBuffers(IntPtr device, ref Rectangle sourceRectangle, IntPtr destinationRectangle, IntPtr overrideWindowHandle);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SwapBuffers(IntPtr device, IntPtr sourceRectangle, ref Rectangle destinationRectangle, IntPtr overrideWindowHandle);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_Clear(IntPtr device, ClearOptions options, ref Vector4 color, float depth, int stencil);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_DrawIndexedPrimitives(IntPtr device, PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount, IntPtr indices, IndexElementSize indexElementSize);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_DrawInstancedPrimitives(IntPtr device, PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount, int instanceCount, IntPtr indices, IndexElementSize indexElementSize);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_DrawPrimitives(IntPtr device, PrimitiveType primitiveType, int vertexStart, int primitiveCount);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetViewport(IntPtr device, ref FNA3D_Viewport viewport);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetScissorRect(IntPtr device, ref Rectangle scissor);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetBlendFactor(IntPtr device, out Color blendFactor);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetBlendFactor(IntPtr device, ref Color blendFactor);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern int FNA3D_GetMultiSampleMask(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetMultiSampleMask(IntPtr device, int mask);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern int FNA3D_GetReferenceStencil(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetReferenceStencil(IntPtr device, int reference);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetBlendState(IntPtr device, ref FNA3D_BlendState blendState);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetDepthStencilState(IntPtr device, ref FNA3D_DepthStencilState depthStencilState);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_ApplyRasterizerState(IntPtr device, ref FNA3D_RasterizerState rasterizerState);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_VerifySampler(IntPtr device, int index, IntPtr texture, ref FNA3D_SamplerState sampler);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_VerifyVertexSampler(IntPtr device, int index, IntPtr texture, ref FNA3D_SamplerState sampler);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public unsafe static extern void FNA3D_ApplyVertexBufferBindings(IntPtr device, FNA3D_VertexBufferBinding* bindings, int numBindings, byte bindingsUpdated, int baseVertex);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetRenderTargets(IntPtr device, IntPtr renderTargets, int numRenderTargets, IntPtr depthStencilBuffer, DepthFormat depthFormat, byte preserveDepthStencilContents);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public unsafe static extern void FNA3D_SetRenderTargets(IntPtr device, FNA3D_RenderTargetBinding* renderTargets, int numRenderTargets, IntPtr depthStencilBuffer, DepthFormat depthFormat, byte preserveDepthStencilContents);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_ResolveTarget(IntPtr device, ref FNA3D_RenderTargetBinding target);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_ResetBackbuffer(IntPtr device, ref FNA3D_PresentationParameters presentationParameters);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_ReadBackbuffer(IntPtr device, int x, int y, int w, int h, IntPtr data, int dataLen);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetBackbufferSize(IntPtr device, out int w, out int h);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern SurfaceFormat FNA3D_GetBackbufferSurfaceFormat(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern DepthFormat FNA3D_GetBackbufferDepthFormat(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern int FNA3D_GetBackbufferMultiSampleCount(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_CreateTexture2D(IntPtr device, SurfaceFormat format, int width, int height, int levelCount, byte isRenderTarget);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_CreateTexture3D(IntPtr device, SurfaceFormat format, int width, int height, int depth, int levelCount);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_CreateTextureCube(IntPtr device, SurfaceFormat format, int size, int levelCount, byte isRenderTarget);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeTexture(IntPtr device, IntPtr texture);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetTextureData2D(IntPtr device, IntPtr texture, int x, int y, int w, int h, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetTextureData3D(IntPtr device, IntPtr texture, int x, int y, int z, int w, int h, int d, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetTextureDataCube(IntPtr device, IntPtr texture, int x, int y, int w, int h, CubeMapFace cubeMapFace, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetTextureDataYUV(IntPtr device, IntPtr y, IntPtr u, IntPtr v, int yWidth, int yHeight, int uvWidth, int uvHeight, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetTextureData2D(IntPtr device, IntPtr texture, int x, int y, int w, int h, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetTextureData3D(IntPtr device, IntPtr texture, int x, int y, int z, int w, int h, int d, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetTextureDataCube(IntPtr device, IntPtr texture, int x, int y, int w, int h, CubeMapFace cubeMapFace, int level, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_GenColorRenderbuffer(IntPtr device, int width, int height, SurfaceFormat format, int multiSampleCount, IntPtr texture);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_GenDepthStencilRenderbuffer(IntPtr device, int width, int height, DepthFormat format, int multiSampleCount);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeRenderbuffer(IntPtr device, IntPtr renderbuffer);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_GenVertexBuffer(IntPtr device, byte dynamic, BufferUsage usage, int sizeInBytes);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeVertexBuffer(IntPtr device, IntPtr buffer);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetVertexBufferData(IntPtr device, IntPtr buffer, int offsetInBytes, IntPtr data, int elementCount, int elementSizeInBytes, int vertexStride, SetDataOptions options);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetVertexBufferData(IntPtr device, IntPtr buffer, int offsetInBytes, IntPtr data, int elementCount, int elementSizeInBytes, int vertexStride);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_GenIndexBuffer(IntPtr device, byte dynamic, BufferUsage usage, int sizeInBytes);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeIndexBuffer(IntPtr device, IntPtr buffer);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetIndexBufferData(IntPtr device, IntPtr buffer, int offsetInBytes, IntPtr data, int dataLength, SetDataOptions options);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetIndexBufferData(IntPtr device, IntPtr buffer, int offsetInBytes, IntPtr data, int dataLength);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_CreateEffect(IntPtr device, byte[] effectCode, int length, out IntPtr effect, out IntPtr effectData);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_CloneEffect(IntPtr device, IntPtr cloneSource, out IntPtr effect, out IntPtr effectData);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeEffect(IntPtr device, IntPtr effect);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_SetEffectTechnique(IntPtr device, IntPtr effect, IntPtr technique);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_ApplyEffect(IntPtr device, IntPtr effect, uint pass, IntPtr stateChanges);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_BeginPassRestore(IntPtr device, IntPtr effect, IntPtr stateChanges);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_EndPassRestore(IntPtr device, IntPtr effect);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FNA3D_CreateQuery(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_AddDisposeQuery(IntPtr device, IntPtr query);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_QueryBegin(IntPtr device, IntPtr query);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_QueryEnd(IntPtr device, IntPtr query);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_QueryComplete(IntPtr device, IntPtr query);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern int FNA3D_QueryPixelCount(IntPtr device, IntPtr query);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsDXT1(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsS3TC(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsBC7(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsHardwareInstancing(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsNoOverwrite(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern byte FNA3D_SupportsSRGBRenderTargets(IntPtr device);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_GetMaxTextureSlots(IntPtr device, out int textures, out int vertexTextures);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern int FNA3D_GetMaxMultiSampleCount(IntPtr device, SurfaceFormat format, int preferredMultiSampleCount);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern void FNA3D_SetStringMarker(IntPtr device, byte* text);

	public unsafe static void FNA3D_SetStringMarker(IntPtr device, string text)
	{
		byte* ptr = SDL.Utf8EncodeHeap(text);
		FNA3D_SetStringMarker(device, ptr);
		Marshal.FreeHGlobal((IntPtr)ptr);
	}

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern void FNA3D_SetTextureName(IntPtr device, IntPtr texture, byte* text);

	public unsafe static void FNA3D_SetTextureName(IntPtr device, IntPtr texture, string text)
	{
		byte* ptr = SDL.Utf8EncodeHeap(text);
		FNA3D_SetTextureName(device, texture, ptr);
		Marshal.FreeHGlobal((IntPtr)ptr);
	}

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr FNA3D_Image_Load(FNA3D_Image_ReadFunc readFunc, FNA3D_Image_SkipFunc skipFunc, FNA3D_Image_EOFFunc eofFunc, IntPtr context, out int width, out int height, out int len, int forceW, int forceH, byte zoom);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FNA3D_Image_Free(IntPtr mem);

	[MonoPInvokeCallback(typeof(FNA3D_Image_ReadFunc))]
	private static int INTERNAL_Read(IntPtr context, IntPtr data, int size)
	{
		Stream stream;
		lock (readStreams)
		{
			stream = readStreams[context];
		}
		byte[] array = new byte[size];
		int num = stream.Read(array, 0, size);
		Marshal.Copy(array, 0, data, num);
		return num;
	}

	[MonoPInvokeCallback(typeof(FNA3D_Image_SkipFunc))]
	private static void INTERNAL_Skip(IntPtr context, int n)
	{
		Stream stream;
		lock (readStreams)
		{
			stream = readStreams[context];
		}
		stream.Seek(n, SeekOrigin.Current);
	}

	[MonoPInvokeCallback(typeof(FNA3D_Image_EOFFunc))]
	private static int INTERNAL_EOF(IntPtr context)
	{
		Stream stream;
		lock (readStreams)
		{
			stream = readStreams[context];
		}
		return (stream.Position == stream.Length) ? 1 : 0;
	}

	public static IntPtr ReadImageStream(Stream stream, out int width, out int height, out int len, int forceW = -1, int forceH = -1, bool zoom = false)
	{
		IntPtr intPtr;
		lock (readStreams)
		{
			intPtr = (IntPtr)(readGlobal++);
			readStreams.Add(intPtr, stream);
		}
		IntPtr result = FNA3D_Image_Load(readFunc, skipFunc, eofFunc, intPtr, out width, out height, out len, forceW, forceH, (byte)(zoom ? 1u : 0u));
		lock (readStreams)
		{
			readStreams.Remove(intPtr);
		}
		return result;
	}

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	private static extern void FNA3D_Image_SavePNG(FNA3D_Image_WriteFunc writeFunc, IntPtr context, int srcW, int srcH, int dstW, int dstH, IntPtr data);

	[DllImport("FNA3D", CallingConvention = CallingConvention.Cdecl)]
	private static extern void FNA3D_Image_SaveJPG(FNA3D_Image_WriteFunc writeFunc, IntPtr context, int srcW, int srcH, int dstW, int dstH, IntPtr data, int quality);

	[MonoPInvokeCallback(typeof(FNA3D_Image_WriteFunc))]
	private static void INTERNAL_Write(IntPtr context, IntPtr data, int size)
	{
		Stream stream;
		lock (writeStreams)
		{
			stream = writeStreams[context];
		}
		byte[] array = new byte[size];
		Marshal.Copy(data, array, 0, size);
		stream.Write(array, 0, size);
	}

	public static void WritePNGStream(Stream stream, int srcW, int srcH, int dstW, int dstH, IntPtr data)
	{
		IntPtr intPtr;
		lock (writeStreams)
		{
			intPtr = (IntPtr)(writeGlobal++);
			writeStreams.Add(intPtr, stream);
		}
		FNA3D_Image_SavePNG(writeFunc, intPtr, srcW, srcH, dstW, dstH, data);
		lock (writeStreams)
		{
			writeStreams.Remove(intPtr);
		}
	}

	public static void WriteJPGStream(Stream stream, int srcW, int srcH, int dstW, int dstH, IntPtr data, int quality)
	{
		IntPtr intPtr;
		lock (writeStreams)
		{
			intPtr = (IntPtr)(writeGlobal++);
			writeStreams.Add(intPtr, stream);
		}
		FNA3D_Image_SaveJPG(writeFunc, intPtr, srcW, srcH, dstW, dstH, data, quality);
		lock (writeStreams)
		{
			writeStreams.Remove(intPtr);
		}
	}
}
