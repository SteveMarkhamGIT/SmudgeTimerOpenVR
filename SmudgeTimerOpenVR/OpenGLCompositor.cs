using OpenTK.Windowing.Desktop;

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;
using OpenTK.Graphics;

using PixelFormat = System.Drawing.Imaging.PixelFormat;
using OpenTK.Graphics.OpenGL;



namespace SmudgeTimerOpenVR
{
	public class OpenGLCompositor : NativeWindow
	{
		private static OpenGLCompositor _instance = null;
		public static OpenGLCompositor Instance
		{
			get
			{
				if (_instance == null)
					_instance = new OpenGLCompositor();

				return _instance;
			}
		}

		public OpenGLCompositor() : base(
			new NativeWindowSettings
			{
				StartVisible = false,
				Title = "OVRSharp Window",
				WindowState = OpenTK.Windowing.Common.WindowState.Minimized
			}
		)
		{ }

		public Bitmap GetMirrorImage(EVREye eye = EVREye.Eye_Left)
		{
			uint textureId = 0;
			var handle = new IntPtr();

			var result = OpenVR.Compositor.GetMirrorTextureGL(eye, ref textureId, handle);
			if (result != EVRCompositorError.None)
				throw new OpenVRSystemException<EVRCompositorError>("Failed to get mirror texture from OpenVR", result);

			OpenVR.Compositor.LockGLSharedTextureForAccess(handle);

			GL.BindTexture(TextureTarget.Texture2D, textureId);

			var height = 0;
			GL.GetTexParameterI(TextureTarget.Texture2D, GetTextureParameter.TextureHeight, out height);

			var width = 0;
			GL.GetTexParameterI(TextureTarget.Texture2D, GetTextureParameter.TextureWidth, out width);

			var bitmap = new Bitmap(width, height);
			var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
				PixelFormat.Format24bppRgb);

			GL.Finish();
			GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte, data.Scan0);

			bitmap.UnlockBits(data);
			bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

			OpenVR.Compositor.UnlockGLSharedTextureForAccess(handle);
			OpenVR.Compositor.ReleaseSharedGLTexture(textureId, handle);

			return bitmap;
		}
	}

}
