using System;

namespace BuzzardLib.Searching
{
    public class StartStopEventArgs : EventArgs
    {
        public StartStopEventArgs(bool monitoring)
        {
            Monitoring = monitoring;
        }

        /// <summary>
        /// True when monitoring
        /// </summary>
        public bool Monitoring
        {
            get;
            private set;
        }
    }
}
