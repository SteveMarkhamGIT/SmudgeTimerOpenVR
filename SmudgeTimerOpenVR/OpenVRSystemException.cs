using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmudgeTimerOpenVR
{
	public class OpenVRSystemException<TError> : Exception
	{
		public readonly TError Error;

		public OpenVRSystemException()
		{
		}

		public OpenVRSystemException(string message)
			: base(message)
		{
		}

		public OpenVRSystemException(string message, Exception inner)
			: base(message, inner)
		{
		}

		public OpenVRSystemException(string message, TError error)
			: this($"{message} ({error})")
		{
			Error = error;
		}
	}
	
}
