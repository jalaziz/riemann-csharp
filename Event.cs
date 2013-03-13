using System;

namespace Riemann.Proto {
	public partial class Event {
	    partial void OnStateChanging(string value)
		{
            if (value.Length > 255)
            {
                throw new ArgumentException("State parameter is too long, must be 255 characters or less", "state");
            }
		}
	}
}