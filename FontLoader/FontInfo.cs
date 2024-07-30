using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontLoader
{
	public struct FontInfo
	{
		public int Width;
		public int Height;
		public Image Image;
		public char Chacter;
		public int PadLeft;
		public int PadRight;
		public int PadTop;
		public int PadBottom;
	}
}
