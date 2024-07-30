using SixLabors.ImageSharp;
using System.Security.Cryptography.X509Certificates;

namespace FontLoader
{
	public abstract class FontLoader
	{
		public abstract Dictionary<char, Image> FontDictionary { get; }

		//public FontLoader(string fileName)
		//{
			
		//}

	}
}