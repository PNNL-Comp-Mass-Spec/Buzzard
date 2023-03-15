using System;

// ReSharper disable UnusedMember.Global

namespace BuzzardWPF.IO.DMS
{
    /// <summary>
    /// Custom exception for reporting errors during stored procedure execution
    /// </summary>
    public class DatabaseStoredProcException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SPName">Name of stored procedure that returned error</param>
        /// <param name="RetCode">Stored procedure return code</param>
        /// <param name="ErrMsg">Error message returned by stored procedure</param>
        public DatabaseStoredProcException(string SPName, int RetCode, string ErrMsg)
        {
            ProcName = SPName;
            ReturnCode = RetCode;
            ErrMessage = ErrMsg;
        }

        public DatabaseStoredProcException()
        {
        }

        public DatabaseStoredProcException(string message) : base(message)
        {
        }

        public DatabaseStoredProcException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Stored procedure return code
        /// </summary>
        public int ReturnCode { get; }

        /// <summary>
        /// Name of stored procedure that returned error
        /// </summary>
        public string ProcName { get; }

        /// <summary>
        /// Error message returned by stored procedure
        /// </summary>
        public string ErrMessage { get; }
    }
}
