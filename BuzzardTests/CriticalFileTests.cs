using System;
using System.Linq;
using System.Text.RegularExpressions;
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
            var width = files.Max(x => x.File.FullName.Length) + 2;
            var format = $"{{0,-{width}}} {{1}}";
            foreach (var file in files)
            {
                Console.WriteLine(format, file.File.FullName, file.GetExtendedName());
            }
        }

        public const string QcDatasetNameRegExString = "^QC(\\d+\\w?)?(_|-).*";
        public const string BlankDatasetNameRegExString = "^BLANK(\\d+\\w?)?(_|-).*";
        private readonly Regex qcDatasetNameRegEx = new Regex(QcDatasetNameRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex blankDatasetNameRegEx = new Regex(BlankDatasetNameRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Test]
        [TestCase("QC_random")]
        [TestCase("qc_random")]
        [TestCase("QC-random")]
        [TestCase("QC01_random")]
        [TestCase("QC2-random")]
        [TestCase("QC04_random")]
        [TestCase("QC5_random")]
        [TestCase("QC13_random")]
        [TestCase("QC5a_random")]
        [TestCase("QC13b_random")]
        [TestCase("QC5ab_random")]
        [TestCase("QC13bs_random")]
        [TestCase("QCb_random")]
        public void TestRegexQC(string testName)
        {
            Assert.IsTrue(qcDatasetNameRegEx.IsMatch(testName), testName);
        }

        [Test]
        [TestCase("Blank_random")]
        [TestCase("blank_random")]
        [TestCase("Blank-random")]
        [TestCase("Blank01_random")]
        [TestCase("Blank1_random")]
        [TestCase("Blank4_random")]
        [TestCase("Blank05-random")]
        [TestCase("Blank4a_random")]
        [TestCase("Blank05b-random")]
        [TestCase("Blank4ab_random")]
        [TestCase("Blank05bc-random")]
        [TestCase("Blankb-random")]
        public void TestRegexBlank(string testName)
        {
            Assert.IsTrue(blankDatasetNameRegEx.IsMatch(testName), testName);
        }

        [Test]
        [TestCase("Blank_random", "Blank(\\d+\\w?)?_random", "^Blank(\\d+\\w?)?[-_]+random")]
        [TestCase("blank-random", "blank(\\d+\\w?)?-random", "^blank(\\d+\\w?)?[-_]+random")]
        [TestCase("QC_random", "QC(\\d+\\w?)?_random", "^QC(\\d+\\w?)?[-_]+random")]
        [TestCase("qc-random", "qc(\\d+\\w?)?-random", "^qc(\\d+\\w?)?[-_]+random")]
        public void TestRegexMatcher(string testName, string addNumber, string finalMatch)
        {
            var startMatch = new Regex("^(?<start>QC|Blank)(?<bar>[-_])", RegexOptions.IgnoreCase);
            var allowNumberString = startMatch.Replace(testName, "${start}(\\d+\\w?)?${bar}");

            var horBarMatch = new Regex("[-_]+");
            var modMatchString = "^" + horBarMatch.Replace(allowNumberString, "[-_]+");

            Assert.AreEqual(addNumber, allowNumberString);
            Assert.AreEqual(finalMatch, modMatchString);
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
