using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmudgeTimerOpenVR
{
	internal class SmudgeTimerOptions
	{
		public bool IsChanged = true;
		public float triggerDistance = 0.01f;
		public string TriggerDistance 
		{ 
			get => $"{triggerDistance}mm"; 
			set 
			{ 
				var v = Utils.ConvertMeasurements(value);
				if (v <= 0) 
					throw new ArgumentOutOfRangeException("Triggert distance is in millimeters and must be greater than 0");
				triggerDistance = (v ?? 0.004f) / 100f;
				IsChanged = true;
			} 
		}
		private int refreshRate = 1000 / 20;
		public int RefreshRate { get => refreshRate; set { if (value <= 0) throw new ArgumentOutOfRangeException("Refresh rate must be greater then 0"); refreshRate = value; } }
		private int ghostSpeed = 100;
		public int GhostSpeed { get => ghostSpeed; set { if (value < 50 || value > 200) throw new ArgumentOutOfRangeException("GhostSpeed must be between 50 and 200"); ghostSpeed = value; IsChanged = true; } }
		private float ghostSpeedMPS = 1.7f;
		public float GhostSpeedMPS { get => ghostSpeedMPS; set => ghostSpeedMPS = value; }
		private int ghostSpeedBPM = 588;
		public int GhostSpeedBPM { get => ghostSpeedBPM; set => ghostSpeedBPM = value; }
		public float timerSize = 1.7f;
		public string TimerSize 
		{ 
			get => $"{timerSize}mm"; 
			set 
			{
				var v = Utils.ConvertMeasurements(value);
				if (v <= 0) 
					throw new ArgumentOutOfRangeException("TimerSize is in millimeters and must be greater than zero.  Default is 100"); 
				timerSize = (v ?? 10f) / 100f; 
			} 
		}
		
		








	}
}
