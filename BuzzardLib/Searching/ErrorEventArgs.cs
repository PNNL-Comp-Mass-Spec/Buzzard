using System;

namespace BuzzardLib.Searching
{
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }
    }
}
