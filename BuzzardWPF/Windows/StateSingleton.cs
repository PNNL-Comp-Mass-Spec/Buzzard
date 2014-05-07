using System;
using System.Security.Cryptography.X509Certificates;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Holds state information 
    /// </summary>
    public static class StateSingleton
    {
        public static event EventHandler WatchingStateChanged;
        public static event EventHandler StateChanged;
        private static bool m_isMonitoring;

        static StateSingleton()
        {
            IsMonitoring = false;
        }
        /// <summary>
        /// Gets or sets whether the system is monitoring 
        /// </summary>
        public static bool IsMonitoring
        {
            get { return m_isMonitoring; }
            set
            {
                m_isMonitoring = value;
                if (WatchingStateChanged != null)
                {
                    WatchingStateChanged(null, null);
                }
            }
        }

        public static void SetState()
        {
            if (StateChanged != null)
            {
                StateChanged(null, null);
            }
        }
    }
}