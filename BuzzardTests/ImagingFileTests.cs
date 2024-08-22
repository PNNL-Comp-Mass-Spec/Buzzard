using System.IO;
using System.Linq;
using System.Xml.XPath;
using NUnit.Framework;

namespace BuzzardTests
{
    [TestFixture]
    internal class ImagingFileTests
    {
        [Test]
        [TestCase(@"\\proto-5\timsTOFFlex02\2024_3\Agilent_tune_mix01\Agilent_tune_mix01.d", false)]
        [TestCase(@"\\proto-5\timsTOFFlex02\2024_3\SRP_Blank_04_29Jul24_Infusion\SRP_Blank_04_29Jul24_Infusion.d", false)]
        [TestCase(@"\\proto-9\timsTOFFlex02_Imaging\2024_3\Fiona_AAO_T2046_timsTOF\Fiona_AAO_T2046_timsTOF.d", true)]
        [TestCase(@"\\proto-9\timsTOFFlex02_Imaging\2024_3\ECM_QC_lung_20240819_BG\ECM_QC_lung_20240819_BG.d", true)]
        public void Test(string path, bool isImaging)
        {
            var result = IsTimsTOFMaldiImagingEnabled(new DirectoryInfo(path));

            Assert.That(result, Is.EqualTo(isImaging));
        }

        private static bool IsTimsTOFMaldiImagingEnabled(DirectoryInfo dotDDirectory)
        {
            if (!dotDDirectory.Exists)
            {
                return false;
            }

            var methods = dotDDirectory.GetDirectories("*.m");
            foreach (var method in methods)
            {
                var maldiConfig = method.GetFiles("Maldi.method").FirstOrDefault();
                if (maldiConfig == null)
                {
                    return false;
                }

                try
                {
                    var xml = new XPathDocument(maldiConfig.FullName);
                    var nav = xml.CreateNavigator();
                    var node = nav.SelectSingleNode("/root/MaldiSource/Enabled");
                    if (node != null && node.IsNode)
                    {
                        var val = node.ValueAsInt;
                        return val != 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
