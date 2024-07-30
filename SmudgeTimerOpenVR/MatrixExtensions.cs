using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

namespace SmudgeTimerOpenVR
{
	public static class MatrixExtension
	{
		//
		// Summary:
		//     Converts a System.Numerics.Matrix4x4 to a Valve.VR.HmdMatrix34_t.
		//
		//     From:
		//     11 12 13 14
		//     21 22 23 24
		//     31 32 33 34
		//     41 42 43 44
		//
		//     To:
		//     11 12 13 41
		//     21 22 23 42
		//     31 32 33 43
		public static HmdMatrix34_t ToHmdMatrix34_t(this Matrix4x4 matrix)
		{
			HmdMatrix34_t result = default(HmdMatrix34_t);
			result.m0 = matrix.M11;
			result.m1 = matrix.M12;
			result.m2 = matrix.M13;
			result.m3 = matrix.M41;
			result.m4 = matrix.M21;
			result.m5 = matrix.M22;
			result.m6 = matrix.M23;
			result.m7 = matrix.M42;
			result.m8 = matrix.M31;
			result.m9 = matrix.M32;
			result.m10 = matrix.M33;
			result.m11 = matrix.M43;
			return result;
		}

		//
		// Summary:
		//     Converts a Valve.VR.HmdMatrix34_t to a System.Numerics.Matrix4x4.
		//
		//     From:
		//     11 12 13 14
		//     21 22 23 24
		//     31 32 33 34
		//
		//     To:
		//     11 12 13 XX
		//     21 22 23 XX
		//     31 32 33 XX
		//     14 24 34 XX
		public static Matrix4x4 ToMatrix4x4(this HmdMatrix34_t matrix)
		{
			return new Matrix4x4(matrix.m0, matrix.m1, matrix.m2, 0f, matrix.m4, matrix.m5, matrix.m6, 0f, matrix.m8, matrix.m9, matrix.m10, 0f, matrix.m3, matrix.m7, matrix.m11, 1f);
		}
	}
}
