using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FontLoader
{
	public class IndividualFileFont : FontLoader
	{
		private Dictionary<char, Image> imageFiles = new Dictionary<char, Image>();
		public override Dictionary<char, Image> FontDictionary => imageFiles;
		public IndividualFileFont(Dictionary<char,string> fileMappings)
		{
			foreach(var i in fileMappings)
			{
				var img = Image.Load(i.Value);
				imageFiles.Add(i.Key, img);
			}
		}
	}
}
