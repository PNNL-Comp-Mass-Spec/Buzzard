using System;
using BuzzardWPF.Management;
using NUnit.Framework;

namespace BuzzardTests
{
    [TestFixture]
    public class CriticalFileTests
    {
        [Test]
        public void TestFind()
        {
            var files = InstrumentCriticalFiles.FindCriticalFiles();
            foreach (var file in files)
            {
                Console.WriteLine(file.File.FullName);
                Console.WriteLine(file.GetExtendedName());
            }
        }

        [Test]
        public void TestCopy()
        {
            var calBackups = InstrumentCriticalFiles.Instance;
            Console.WriteLine("Backup Directory: \"{0}\"", calBackups.BackupDir);
            calBackups.CopyCriticalFilesToServer();
        }
    }
}
