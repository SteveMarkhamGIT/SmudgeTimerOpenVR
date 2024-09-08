using FontLoader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace SmudgeTimerGenerator
{
	public class SmudgeTimerHost:IDisposable
	{
		const int oWidth = 500;
		const int oHeight = 500;
		const int oMidX = oWidth / 2;
		const int oMidY = oHeight / 2;
		const double MaxSeconds = 180;

		const double BaseMSPerStep = 521.7d;
		double MSPerStep = BaseMSPerStep;
		
		public double GhostSpeedMPS
		{
			get
			{
				return 1 / (MSPerStep / 100);
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(GhostSpeedMPS), "Value must be between 0.1 and 1000.  Default is 1.7");
				MSPerStep = value * 50;

			}
		}
		public double GhostSpeedBPM
		{
			get
			{
				return MSPerStep * 100;
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(GhostSpeedBPM), "Value must be between 0.1 and 1000.  Default is 588");
				MSPerStep = value / 200;
			}
		}

		public double GhostSpeedPct
		{
			get
			{
				return MSPerStep / BaseMSPerStep * 100;
			}
			set 
			{
				if (value < 50 || value > 200)
					throw new ArgumentOutOfRangeException(nameof(GhostSpeedPct), "Value must be between 50 and 200");
				MSPerStep = BaseMSPerStep * (100d / value);
			}
		}

		FontLoader.FontLoader fontLoader;
		Dictionary<char, SixLabors.ImageSharp.Image> fontDict;
		Image<Rgba32> ghostRing;
		Image<Rgba32> middleLayer;
		Image<Rgba32> ghostHand;
		Image<Rgba32> footPrint;
		DateTime appStartTime = DateTime.Now;
		DateTime? startTime = null;
		DateTime? endTime = null;

		Image<Rgba32> buf;
		private Image<Rgba32> outputImage;
		private bool isFlashing;
		private Color flashColor;
		private DateTime? flashStart = null;
		private const int flashMS = 500;


		public Image<Rgba32> OutputImage => outputImage;
		
		public string ResouceLocation { get; private set; }

		public SmudgeTimerHost(string resouceLocation)
		{
			ResouceLocation = resouceLocation;

		}
		public void Initialize()
		{
			buf = new Image<Rgba32>(oWidth, oHeight);
			LoadFonts();
			LoadImages();
		}
		private void LoadImages()
		{
			ghostRing = Image<Rgba32>.Load<Rgba32>(ResourcePath("Images\\PhasmoGhostRing.webp"));
			middleLayer = Image<Rgba32>.Load<Rgba32>(ResourcePath("Images\\PhasmoMiddleLayer.webp"));
			ghostHand = Image<Rgba32>.Load<Rgba32>(ResourcePath("Images\\PhasmoHand.webp"));
			footPrint = Image<Rgba32>.Load<Rgba32>(ResourcePath("Images\\footprint.webp"));
		}

		private string ResourcePath(string resouceFN)
		{
			return System.IO.Path.Combine(ResouceLocation, resouceFN);
		}

		private void LoadFonts()
		{
			var fDict = new Dictionary<char, string>();

			fDict.Add('0', ResourcePath("Fonts\\c-0.webp"));
			fDict.Add('1', ResourcePath("Fonts\\c-1.webp"));
			fDict.Add('2', ResourcePath("Fonts\\c-2.webp"));
			fDict.Add('3', ResourcePath("Fonts\\c-3.webp"));
			fDict.Add('4', ResourcePath("Fonts\\c-4.webp"));
			fDict.Add('5', ResourcePath("Fonts\\c-5.webp"));
			fDict.Add('6', ResourcePath("Fonts\\c-6.webp"));
			fDict.Add('7', ResourcePath("Fonts\\c-7.webp"));
			fDict.Add('8', ResourcePath("Fonts\\c-8.webp"));
			fDict.Add('9', ResourcePath("Fonts\\c-9.webp"));
			fDict.Add(':', ResourcePath("Fonts\\colon.webp"));
			fDict.Add('.', ResourcePath("Fonts\\period.webp"));

			fontLoader = new IndividualFileFont(fDict);
			fontDict = fontLoader.FontDictionary;
		}


		public void Dispose()
		{
			
		}
		public void Refresh()
		{
			
			if (startTime.HasValue)
			{
				if (endTime.HasValue)
				{
					BuildImages(endTime.Value.Subtract(startTime.Value));
				}
				else
				{
					BuildImages(DateTime.Now.Subtract(startTime.Value));
				}
			}
			else
			{
				BuildImages(TimeSpan.FromSeconds(0));
			}
			if (isFlashing)
			{
				DoFlash();
			}
		}
		public void StartFlash(Color color)
		{
			if (!isFlashing)
			{
				flashStart = DateTime.Now;
				isFlashing = true;
				flashColor = color;
			}
		}
		private void DoFlash()
		{
			if (flashStart.HasValue)
			{
				float flashPct = (float)DateTime.Now.Subtract(flashStart.Value).TotalMilliseconds / flashMS;
				if(flashPct > 1)
				{
					isFlashing = false;
					flashStart = null;
					return;
				}
				var flashVisPct = (float)Math.Sin(flashPct * Math.PI);
				var targetColor = flashColor.ToPixel<Rgba32>();
				var targetRed = targetColor.R;
				var targetGreen = targetColor.G;
				var targetBlue = targetColor.B;


				for(int y=0;y<outputImage.Height;y++)
				{ 
					for(int x=0;x<outputImage.Width;x++)
					{
						var p = outputImage[x,y];
						if(p.A>0)
						{
							p.R = InterpolateValue(p.R, targetRed, flashVisPct);
							p.G = InterpolateValue(p.G, targetGreen, flashVisPct);
							p.B = InterpolateValue(p.B, targetBlue, flashVisPct);
							outputImage[x,y] = p;
						}
					}
				}
			}
			else
			{
				isFlashing = false;
			}
			
		}
		private byte InterpolateValue(byte value, byte targetValue, float pct)
		{
			var amt = (((float)targetValue - value) * pct) + value;
			if (amt < 0)
				amt = 0;
			else if (amt > byte.MaxValue)
				amt = byte.MaxValue;
			return (byte)amt;
		}

		public void StartStop()
		{
			if (!startTime.HasValue || (startTime.HasValue && endTime.HasValue))
			{
				startTime = DateTime.Now;
				endTime = null;
				StartFlash(Color.Green);
			}
			else
			{
				endTime = DateTime.Now;
				StartFlash(Color.Red);
			}
		}
		public void Reset()
		{
			startTime = null;
			endTime = null;
		}


		private void BuildImages(TimeSpan t)
		{
			var elapsed = t.TotalSeconds;
			buf = ghostRing.Clone<Rgba32>(x => { });

			if (elapsed == 0 || elapsed > MaxSeconds)
			{
				elapsed = MaxSeconds;

			}
			var revolutions = Math.Floor(elapsed) / MaxSeconds;
			var angleRadians = (revolutions * Math.PI * 2 - Math.PI / 2) % (Math.PI * 2);
			var angleDeg = revolutions * 360;
			if (elapsed < MaxSeconds)
				//DrawFader(-Math.PI / 2,(angleRadians ), buf as Image<Rgba32>); //- Math.PI / 2
				DrawFadeDeg(270, angleDeg - 90, buf);
			buf.Mutate(m => m.DrawImage(middleLayer, 1f));

			DrawTimer(t, buf);

			DrawFootprints(buf);

			if (elapsed != 0 && elapsed < MaxSeconds)
			{

				//var mask = new Image<Rgba32>(oWidth, oHeight);
				var af = new AffineTransformBuilder();
				af.AppendRotationRadians((float)angleRadians, new System.Numerics.Vector2(oMidX, oMidY));
				buf.Mutate(m => m.DrawImage(ghostHand.Clone(c => c.Transform(af)), 1f));

				//var rotHand = ghostHand.Clone(mh => mh.Rotate((float)angleDeg - 90f, new BicubicResampler()));
				//var newCenterX = (int)Math.Round(rotHand.Width / 2f - 0.1);
				//var newCenterY = (int)Math.Round(rotHand.Height / 2f - 0.1);
				//var cropRect = new SixLabors.ImageSharp.Rectangle(
				//		newCenterX - oMidX,
				//		newCenterY - oMidY,
				//		newCenterX + oMidX,
				//		newCenterY + oMidY);
				//rotHand.Mutate(m => m.Crop(cropRect));
				//buf.Mutate(m => m.DrawImage(rotHand, 1f));
			}
			else
			{
				buf.Mutate(m => m.DrawImage(ghostHand.Clone(mh => mh.Rotate(-90f)), 1f));
			}

			outputImage = buf.Clone(x => { });
			
		}

		private void DrawFadeDeg(int offsetDeg, double angleDeg, Image<Rgba32> buf)
		{
			const byte dimmerAmt = 4;
			for (int y = 1; y < oHeight; y++)
				for (int x = 0; x < oWidth; x++)
				{
					var p = buf[x, y];
					var mx = (double)x - oMidX;
					var my = (double)y - oMidY;
					var curAngle = ((angleDeg < offsetDeg ? angleDeg + 360 : angleDeg) - offsetDeg);

					if (p.A > 0)
					{
						var pAngle = -Math.Atan(mx / (my == 0 ? 0.1d : my)) * 180 / Math.PI;

						if (mx < 0 && my < 0)
							pAngle += 360;
						if (my > 0)
							pAngle += 180;

						if (mx == -210 && my == -10)
							DoNothing();  //272
						if (mx == -10 && my == -210)
							DoNothing(); //357
						if (mx == 210 && my == 10)
							DoNothing(); //7
						if (mx == -210 && my == 10)
							DoNothing();
						//if (mx < 0 && my < 0)
						//	pAngle += (Math.PI * 2);
						//else if (mx >= 0 && my > 0)
						//	pAngle += Math.PI - pAngle;
						//pAngle = (pAngle + (2 * Math.PI) % (2 * Math.PI));
						//if (pAngle < minAngleRadians)
						//pAngle += (Math.PI * 2);
						//pAngle += (Math.PI * 2);
						//pAngle %= (Math.PI * 2);


						var pOffsetAngle = ((pAngle < offsetDeg ? pAngle + 360 : pAngle) - offsetDeg);

						//if (pAngle > curAngle)
						if (pOffsetAngle > curAngle)
						{
							p.R = ((byte)(p.R / dimmerAmt));
							p.G = ((byte)(p.G / dimmerAmt));
							p.B = ((byte)(p.B / dimmerAmt));

						}
						buf[x, y] = p;
					}
				}
		}

		private void DoNothing()
		{

		}

		private void DrawFader(double minAngleRadians, double angleRadians, Image<Rgba32> buf)
		{

			//       360
			//        |
			// 270 -------- 90
			//        |
			//       180

			//        0
			//        |
			// -90 -------- 90
			//        |
			//        0
			const byte dimmerAmt = 4;
			for (int y = 1; y < oHeight; y++)
				for (int x = 0; x < oWidth; x++)
				{
					var p = buf[x, y];
					var mx = (double)x - oMidX;
					var my = (double)y - oMidY;
					if (p.A > 0)
					{
						var pAngle = -Math.Atan(mx / (my == 0 ? 0.1d : my));
						if (mx < 0 && my < 0)
							pAngle += (Math.PI * 2);
						else if (mx >= 0 && my > 0)
							pAngle += Math.PI - pAngle;
						//pAngle = (pAngle + (2 * Math.PI) % (2 * Math.PI));
						//if (pAngle < minAngleRadians)
						//pAngle += (Math.PI * 2);
						//pAngle += (Math.PI * 2);
						//pAngle %= (Math.PI * 2);

						if (pAngle > angleRadians)
						{
							p.R = ((byte)(p.R / dimmerAmt));
							p.G = ((byte)(p.G / dimmerAmt));
							p.B = ((byte)(p.B / dimmerAmt));

						}
						buf[x, y] = p;
					}
				}
		}

		private void DrawFootprints(Image<Rgba32> buf)
		{
			var ms = DateTime.Now.Subtract(appStartTime).TotalMilliseconds % (MSPerStep * 2);

			var fader = Math.Sin(ms / (MSPerStep * 2) * Math.PI * 2);
			double f1, f2;
			if (fader > 0)
			{
				f1 = fader;
				f2 = 0;
				buf.Mutate(m => m.DrawImage(footPrint.Clone(m2 => m2.Flip(FlipMode.Horizontal)), new SixLabors.ImageSharp.Point(200, 100), (float)f1));
			}
			else
			{
				f1 = 0;
				f2 = -fader;
				buf.Mutate(m => m.DrawImage(footPrint, new SixLabors.ImageSharp.Point(255, 100), (float)f2));
			}

		}

		private void DrawTimer(TimeSpan t, Image<Rgba32> buf)
		{
			const string formatString = @"mmssf";
			var displayTime = t.ToString(formatString);
			var hr = displayTime[0];
			buf.Mutate(m => m.DrawImage(fontDict[displayTime[0]], new SixLabors.ImageSharp.Point(100, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict[displayTime[1]], new SixLabors.ImageSharp.Point(150, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict[':'], new SixLabors.ImageSharp.Point(205, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict[displayTime[2]], new SixLabors.ImageSharp.Point(215, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict[displayTime[3]], new SixLabors.ImageSharp.Point(265, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict['.'], new SixLabors.ImageSharp.Point(320, 270), 1f));
			buf.Mutate(m => m.DrawImage(fontDict[displayTime[4]], new SixLabors.ImageSharp.Point(330, 270), 1f));

		}
	}
}
