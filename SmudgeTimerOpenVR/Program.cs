namespace SmudgeTimerOpenVR
{
	using Microsoft.Extensions.Configuration;
	using OpenTK.Graphics.OpenGL;
	using System.Runtime.CompilerServices;
	using System.Security.Cryptography.X509Certificates;
	using Valve.VR;
	

	internal class Program
	{
		private static SmudgeTimerOverlay? ovr = null;
		static void Main(string[] args)
		{
			Console.WriteLine(@"
Welcome to the Phasmo Smudge Timer for SteamVR!

To start or stop the timer, bring the front of your controllers together to within the trigger distance.
The first mark is the time it takes an unsmudged demon to hunt (20s).
The second mark is the time every other ghost takes between hunts (25s)
The third mark is the time it takes a demon to hunt after being smudged (1:00) (as measured from the time of smudging)
The fourth mark is the smudge time for every other ghost (1:30), except spirits.
Spirits take the full 3:00 after being smudged.
The footprints measure out how fast a standard ghost moves when it doesn't have a target (1.7 m/s).
The config.json file can be edited to store your preferences.

Commands and their defaults:
GhostSpeed=100  --affects the footprint speed
TimerSize=10mm  --changes the size of the ui
TriggerDistance=0.4mm  --chages how close your controllers need to be to start/stop the timer
exit   --termintaes this program

");
			var builder = new ConfigurationBuilder()
				.AddJsonFile("config.json", true, true)
				.AddCommandLine(args)
				.Build();
			opts = builder.Get<SmudgeTimerOptions>();

			
			var clthread = new Thread(() => CommandLoop());
			
			clthread.Start();
			while(!exit)
			{
				DoWork(null);
				Thread.Sleep(TimeSpan.FromMilliseconds(opts != null ? opts.RefreshRate : 50));
			}
			}
		static bool exit = false;
		private static void CommandLoop()
		{
			
			while (!exit)
			{
				var cmd = Console.ReadLine();
				if (cmd == "exit" || cmd == "quit" || cmd == "x")
				{
					exit = true;
					break;
				}
				else if (cmd == "?" || cmd == "help")
				{
					Console.WriteLine($"ghostspeed=100{Environment.NewLine}triggerdistance=5{Environment.NewLine}refreshrate=20");
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(cmd) && cmd.Contains("="))
					{
						var sa = cmd.ToLower().Replace(" ","").Split('=');

						var cmdPart = sa[0];
						var valuePart = sa[1];
						try
						{
							if (opts != null)
							{
								switch (cmdPart)
								{
									case "gs":
									case "ghostspeed":
										opts.GhostSpeed = Convert.ToInt32(valuePart); 
										break;
									case "td":
									case "triggerdistance":
										opts.TriggerDistance = valuePart; 
										break;
									case "refreshrate":
										opts.RefreshRate = Convert.ToInt32(valuePart);
										break;
									case "timersize":
									case "size":
											opts.TimerSize = valuePart; 
										break;
								}
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					}

				}

			}
		}
		
		public static bool refreshing=false;
		private static SmudgeTimerOptions? opts;
		public static Thread? MainThread = null;
		private static bool isFirstRun = true;

		public static void DoWork(object? state)
		{
			if (isFirstRun && opts != null)
			{
				isFirstRun = false;
				EVRInitError pError = new(); ;
				var sys = OpenVR.Init(ref pError, EVRApplicationType.VRApplication_Overlay, "");

				if (pError != EVRInitError.None)
				{
					throw new OpenVRSystemException<EVRInitError>(pError.ToString());
				}
				var inst = OpenGLCompositor.Instance;
				ovr = new SmudgeTimerOverlay("SmudgeTimer", "Smudge Timer", sys, inst);
				ovr.GhostSpeedPct = opts.GhostSpeed;
				ovr.TriggerDistance = opts.triggerDistance;
				ovr.WidthInMeters = opts.timerSize;
				opts.IsChanged = false;
			}
			else if (!refreshing)
			{

				refreshing = true;
				//MainThread?.in

				ovr?.Update();
				refreshing = false;
			}
			if (opts != null && ovr != null && opts.IsChanged)
			{
				ovr.GhostSpeedPct = opts.GhostSpeed;
				ovr.TriggerDistance = opts.triggerDistance;
				ovr.WidthInMeters = opts.timerSize;
				opts.IsChanged = false;
			}
			
		}
	}
}
