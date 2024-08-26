using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SmudgeTimerGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Valve.VR;
using static System.Net.Mime.MediaTypeNames;

namespace SmudgeTimerOpenVR
{
	public class SmudgeTimerOverlay
	{
		private ulong _overlayHandle;
		uint TrackedDeviceIndex = 0;
		private bool ControllersTouching = false;
		private bool ControllersWereTouching = false;
		bool TrackingIsFresh = false;

		private uint? lIndex = null;
		private uint? rIndex = null;
		private uint? hmdIndex = null;

		private CVRSystem _sys;
		private bool disableErrors;
		Texture_t overlayTexture;
		private uint ovrWidth;
		private uint ovrHeight;

		public string Key { get; }
		public string Name { get; }


		private double ghostSpeed = 1.7;
		private double ghostSpeedPct = 100;
		private const float lockoutMS = 500f;
		private DateTime inputLockoutUntil = DateTime.MinValue;

		public double GhostSpeedPct 
		{
			get
			{
				return (h != null) ? h.GhostSpeedPct : ghostSpeedPct;
			}
			set 
			{ 
				ghostSpeedPct = value; 
				h.GhostSpeedPct = value;
			}
		}
		private float triggerDistance = 0.005f;
		public float TriggerDistance { get => triggerDistance; set => triggerDistance = value; }

		SmudgeTimerHost h = new SmudgeTimerHost(Utils.GetResourcePath());
		private OpenGLCompositor _instance;
		private uint textureHandle;
		private bool debugMode = false;

		public SmudgeTimerOverlay(string key, string name, CVRSystem sys, OpenGLCompositor instance)
		{
			if (OpenVR.Overlay == null)
			{
				throw new NullReferenceException("OpenVR has not been initialized. Please initialize it by instantiating a new Application.");
			}

			EVROverlayError eVROverlayError = (OpenVR.Overlay.CreateOverlay(key, name, ref _overlayHandle));
			if (eVROverlayError != 0)
			{
				throw new OpenVRSystemException<EVROverlayError>($"Could not initialize overlay: {eVROverlayError}", eVROverlayError);
			}

			//_instance = OpenGLCompositor.Instance;
			
			Key = key;
			Name = name;
			_sys = sys;
			Init();
			_instance = instance;
		}
		public void Init()
		{
			WidthInMeters = 0.125f;
			

			TrackedDeviceIndex = LIndex.GetValueOrDefault(0);

			InitTrackedDevice();
			//TrackedDeviceIndex = (uint)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.Invalid);

			var xRot = DegreesToRadians(140f);
			var yRot = DegreesToRadians(0f);
			var zRot = DegreesToRadians(-90f);

			//var q = Quaternion.CreateFromYawPitchRoll(yRot, xRot, zRot);

			var rotX = Matrix4x4.CreateRotationX(xRot);
			//var rotY = Matrix4x4.CreateRotationY(yRot);
			var rotZ = Matrix4x4.CreateRotationZ(zRot);
			var trans = Matrix4x4.CreateTranslation(-0.08f, 0.12f, 0.2f);
			var finalMatrix = rotX * rotZ * Matrix4x4.CreateRotationX(DegreesToRadians(10f)) * trans;

			myTransform(finalMatrix
				.ToHmdMatrix34_t(), LIndex.GetValueOrDefault(0));


			InitTimer();
			//SetTextureRaw(overlayTexture);
			AssertNoError(OpenVR.Overlay.ShowOverlay(_overlayHandle));
		}
		

		private void InitTimer()
		{
			h.Initialize();
			h.Refresh();
			ovrWidth = (uint)h.OutputImage.Width;
			ovrHeight = (uint)h.OutputImage.Height;
			InitTexture(h.OutputImage);
			//overlayTexture = (float[])h.OutputImage.ToFloatArray();
			
			//h.OutputImage.Dispose();
		}

		private void InitTexture(Image<Rgba32> outputImage)
		{
			
			textureHandle = (uint)GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, textureHandle);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new[] { 1f, 0f, 1f });

			var parr = outputImage.ToFloatArray();
			unsafe
			{

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, h.OutputImage.Width, h.OutputImage.Height, 0, PixelFormat.Rgba, PixelType.Float, parr);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			}

			overlayTexture = new Valve.VR.Texture_t() { eColorSpace = EColorSpace.Auto, handle = (IntPtr)textureHandle, eType = ETextureType.OpenGL };
			parr = null;
			h.OutputImage.Dispose();
			SetTexture(overlayTexture);
			
		}
		public void Update()
		{
			CheckHandPositions();
			h.Refresh();
			RefreshTexture(h.OutputImage);
			
			TrackingIsFresh = false;
		}
		private void RefreshTexture(Image<Rgba32> outputImage)
		{
			var parr = outputImage.ToFloatArray();
			unsafe
			{
				GL.BindTexture(TextureTarget.Texture2D, textureHandle);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, h.OutputImage.Width, h.OutputImage.Height, PixelFormat.Rgba, PixelType.Float, parr);

			}
			SetTexture(overlayTexture);
			parr = null;
			h.OutputImage.Dispose();
			
			
		}
		public void SetTexture(Texture_t texture)
		{
			AssertNoError(OpenVR.Overlay.SetOverlayTexture(_overlayHandle, ref texture));
		}
		public void SetTextureRaw(float[] texture)
		{
			unsafe
			{
				fixed(float* txtptr = texture)
				{
					AssertNoError(OpenVR.Overlay.SetOverlayRaw(_overlayHandle, (nint)txtptr, ovrWidth, ovrHeight, sizeof(float) * 2));
				}
			}
		}
		public void SetTextureRaw(byte[] texture)
		{
			unsafe
			{
				fixed (byte* txtptr = texture)
				{
					AssertNoError(OpenVR.Overlay.SetOverlayRaw(_overlayHandle, (nint)txtptr, ovrWidth, ovrHeight, 4));
				}
			}
		}
		private static void AssertNoError(EVROverlayError error)
		{
			if (error == EVROverlayError.None)
			{
				return;
			}

			throw new OpenVRSystemException<EVROverlayError>($"An error occurred within an Overlay. {error}", error);
		}
		public float WidthInMeters
		{
			get
			{
				float pfWidthInMeters = 0f;
				AssertNoError(OpenVR.Overlay.GetOverlayWidthInMeters(_overlayHandle, ref pfWidthInMeters));
				return pfWidthInMeters;
			}
			set
			{
				AssertNoError(OpenVR.Overlay.SetOverlayWidthInMeters(_overlayHandle, value));
			}
		}
		public uint? LIndex
		{
			get
			{
				if (lIndex == null || rIndex == lIndex)
					lIndex = (uint)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
				if (lIndex > 16)
					lIndex = null;
				return lIndex;
			}
			set => lIndex = value;
		}
		public uint? RIndex
		{
			get
			{
				if (rIndex == null || rIndex == lIndex)
					rIndex = (uint)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
				if (rIndex > 16)
					rIndex = null;
				return rIndex;
			}
			set => rIndex = value;
		}
		public uint? HMDIndex
		{
			get
			{
				if (hmdIndex == null)
					hmdIndex = (uint)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.Invalid);
				if (hmdIndex > 16)
					hmdIndex = null;
				return hmdIndex;
			}
			set => hmdIndex = value;
		}

		public bool DebugMode { get => debugMode; set => debugMode = value; }

		private void InitTrackedDevice()
		{
			TrackedDeviceIndex = (uint)OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
		}
		private void myTransform(HmdMatrix34_t m, uint trackedDeviceIndex)
		{
			EVROverlayError err;
			if (TrackedDeviceIndex == 0)
			{
				err = OpenVR.Overlay.SetOverlayTransformAbsolute(_overlayHandle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref m);

			}
			else
			{
				err = OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(_overlayHandle, trackedDeviceIndex, ref m);
			}
			if (err != EVROverlayError.None && !disableErrors)
			{
				Console.WriteLine($"Error: {err}");
				disableErrors = true;
			}
		}
		private static float DegreesToRadians(float degrees)
		{
			return (float)(degrees * (Math.PI / 180f));
		}
		

		

		private void CheckHandPositions()
		{
			UpdatePoses();
		}
		private void UpdatePoses()
		{
			if (inputLockoutUntil < DateTime.Now)
			{

				TrackedDevicePose_t[] poses = new TrackedDevicePose_t[16];
				//GetRawTrackedDevicePoses
				UpdateTracking(poses);

				var lpose = poses[LIndex.GetValueOrDefault(2)];
				var rpose = poses[RIndex.GetValueOrDefault(3)];
				float dist = GetDistance(lpose, rpose);
				var l1 = lpose.mDeviceToAbsoluteTracking;
				var r1 = rpose.mDeviceToAbsoluteTracking;


				ControllersTouching = dist < TriggerDistance;
				if (ControllersTouching && !ControllersWereTouching)
				{
					if(DebugMode)	
						Console.WriteLine($"controllers now touching: {dist}");
					ControllersWereTouching = true;
					inputLockoutUntil = DateTime.Now.AddMilliseconds(lockoutMS);  //prevents repeated start/stops when controllers are held close to trigger distance
					StartStop();

				}
				else if (!ControllersTouching && ControllersWereTouching)
				{
					if (DebugMode)
						Console.WriteLine($"controllers no longer touching: {dist}");
					ControllersWereTouching = false;
					//StartStop();
				}
			}

		}

		private void UpdateTracking(TrackedDevicePose_t[] poses)
		{
			if (!TrackingIsFresh)
			{
				_sys.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);
				TrackingIsFresh = true;
			}
		}

		private float GetDistance(TrackedDevicePose_t lpose, TrackedDevicePose_t rpose)
		{
			var l1 = lpose.mDeviceToAbsoluteTracking;
			var r1 = rpose.mDeviceToAbsoluteTracking;
			var l44 = l1.ToMatrix4x4();
			var r44 = r1.ToMatrix4x4();
			var ladjusted = l44.Translation + (new System.Numerics.Vector3(0.08f, -0.12f, -0.2f));
			//var res = l44 - r44;
			//Matrix4x4.DeKse(l44, out var lscale, out var lrot, out var ltrans);
			//Matrix4x4.DeKse(r44, out var rscale, out var rrot, out var rtrans);
			//var dist = System.Numerics.Vector3.Distance(ltrans, rtrans);
			//var dist = (ladjusted - r44.Translation).LengthSquared();
			var dist = (l44.Translation - r44.Translation).LengthSquared();
			return dist;
		}
		internal void StartStop()
		{

			if (h != null)
			{
				h.StartStop();
			}
		}
	}
}
