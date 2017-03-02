﻿using System;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Holds state information
    /// </summary>
    public static class StateSingleton
    {
        public static event EventHandler CreatingTriggerFilesStateChanged;
        public static event EventHandler WatchingStateChanged;

        [ObsoleteAttribute("Previously used for debugging")]
        public static event EventHandler StateChanged;

        private static bool m_isCreatingTriggerFiles;

        private static bool m_isMonitoring;

        static StateSingleton()
        {
            IsCreatingTriggerFiles = false;
            IsMonitoring = false;
        }

        public static bool IsCreatingTriggerFiles
        {
            get { return m_isCreatingTriggerFiles; }
            set
            {
                m_isCreatingTriggerFiles = value;
                CreatingTriggerFilesStateChanged?.Invoke(null, null);
            }
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
                WatchingStateChanged?.Invoke(null, null);
            }
        }

        [ObsoleteAttribute("Previously used for debugging")]
        public static void SetState()
        {
            StateChanged?.Invoke(null, null);
        }
    }
}