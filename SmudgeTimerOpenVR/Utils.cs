using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmudgeTimerOpenVR
{
	internal static class Utils
	{
		public static string GetResourcePath()
		{
#if DEBUG
			var dir = Path.Combine(Directory.GetCurrentDirectory(),@"..\..\..\Resources");
#else
			var dir = Path.Combine(Directory.GetCurrentDirectory(), @".\Resources");
#endif
			return dir;

		}
		public static float? ConvertMeasurements(string input)
		{
			if (string.IsNullOrEmpty(input))
				return null;
			else if (input.EndsWith("in"))
			{
				return Convert.ToSingle(input.Substring(0, input.Length - 2)) * 25.4f;
			}
			else if (input.EndsWith("mm"))
			{
				return Convert.ToSingle(input.Substring(0, input.Length - 2));
			}
			else
				return Convert.ToSingle(input);
		}
		public static float[] ToFloatArray(this SixLabors.ImageSharp.Image<Rgba32> imageIn)
		{
			const int stride = 4;
			var ret = new float[imageIn.Width * imageIn.Height * stride];

			for (int y = 0; y < imageIn.Height; y++)
			{
				for (int x = 0; x < imageIn.Width; x++)
				{
					var p = imageIn[x, imageIn.Height - y - 1];
					float alpha = (float)p.A / byte.MaxValue;
					ret[y * stride * imageIn.Width + (x * stride)] = (float)p.R / byte.MaxValue;
					ret[y * stride * imageIn.Width + (x * stride) + 1] = (float)p.G / byte.MaxValue;
					ret[y * stride * imageIn.Width + (x * stride) + 2] = (float)p.B / byte.MaxValue;
					ret[y * stride * imageIn.Width + (x * stride) + 3] = (float)p.A / byte.MaxValue;
				}
			}
			return ret;

		}
		public static byte[] ToByteArray(this SixLabors.ImageSharp.Image<Rgba32> imageIn)
		{
			const int stride = 4;
			var ret = new byte[imageIn.Width * imageIn.Height * stride];

			for (int y = 0; y < imageIn.Height; y++)
			{
				for (int x = 0; x < imageIn.Width; x++)
				{
					var p = imageIn[x, imageIn.Height - y - 1];
					float alpha = (float)p.A / byte.MaxValue;
					ret[y * stride * imageIn.Width + (x * stride)] = p.R;
					ret[y * stride * imageIn.Width + (x * stride) + 1] = p.G;
					ret[y * stride * imageIn.Width + (x * stride) + 2] = p.B;
					ret[y * stride * imageIn.Width + (x * stride) + 3] = p.A;
				}
			}
			return ret;
		}

	}
}
