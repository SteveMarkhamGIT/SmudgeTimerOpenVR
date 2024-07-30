using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontLoader
{
	public class FixedWidthBitmapFont : FontLoader
	{
		private Dictionary<char, Image> imageDictionary = new Dictionary<char, Image>();
		public override Dictionary<char, Image> FontDictionary => imageDictionary;

		public FixedWidthBitmapFont(string fileName, int charactersPerRow, string characters, int xgap = 0, int ygap = 0) 
		{
			var img = SixLabors.ImageSharp.Image.Load(fileName);
			int totalChars = characters.Length;
			int charWidth = (int)Math.Ceiling((double)(img.Width - (xgap * (charactersPerRow - 1))) / charactersPerRow);
			int charRows = (int)Math.Ceiling((double)totalChars / charactersPerRow);
			int charHeight = (int)Math.Ceiling((double)(img.Height - (ygap * (charRows - 1))) / charRows);

			for (int iy = 0, i = 0; iy < charRows; iy++)
			{
				for (int ix = 0; ix < charactersPerRow; ix++, i++)
				{

					var r = new SixLabors.ImageSharp.Rectangle()
					{
						X = ix * (charWidth + xgap),
						Y = iy * (charHeight + ygap),
						Width = charWidth,
						Height = charHeight
					};
					var key = characters[i];

					imageDictionary.Add(key, img.Clone(x => x.Crop(r)));
				}
			}

		}

		
	}
}
