using System;

namespace BuzzardTests
{
    static class ProgramTest
    {


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            BuzzardTests.TrieTest t = new TrieTest();
            t.TestSetup();
            t.TestTrieBuilding();
                 
        }
    }
}
