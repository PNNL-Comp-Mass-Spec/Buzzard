using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using LcmsNetDataClasses.Data;
using LcmsNetDataClasses;

using NUnit.Core;
using NUnit;
using NUnit.Util;
using NUnit.Framework;

namespace BuzzardTests
{    
    [TestFixture]
    public class TrieTest
    {
        private List<classDMSData> m_datasets;
        private List<string> m_datasetNames;
        private List<string> m_fileNames;
        private Dictionary<int, string> m_valids;

        [TestFixtureSetUp]
        public void TestSetup()
        {
            m_datasets      = new List<classDMSData>();
            m_datasetNames  = new List<string>();
            m_datasetNames.Add("SysVirol_SCL012_icSARS-DORF6_36h_1_Met");

            foreach(string name in m_datasetNames)
            {
                classDMSData dms = new classDMSData();
                dms.RequestName  = name;
 
                m_datasets.Add(dms);
            }

            m_fileNames = new List<string>();
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\Blank01.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\Blank02.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\Blank03.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\Blank04.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\Blank05.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\FAME.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_0h_3_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_12h_3_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_24h_2_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_36h_1_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_48h_3_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS-DORF6_60h_4_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_0h_6_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_12h_5_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_24h_6_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_36h_5_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_48h_3_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_icSARS_60h_4_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_0h_2_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_12h_6_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_24h_1_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_36h_5_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_48h_6_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_60h_2_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\ErikaZink\SysVirol\SysVirol_SCL012_mock_72h_2_Met.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_1.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_2.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_3.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_4.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_5.D\DATA.MS");
            m_fileNames.Add(@"m:\buzzardtestdata\GCMS_DMS_uploading_6.D\DATA.MS");


            m_valids = new Dictionary<int, string>();
            foreach(string name in m_datasetNames)
            {
                for (int j = 0; j < m_fileNames.Count; j++)
                {
                    if (m_fileNames[j].Contains(name))
                    {
                        m_valids.Add(j, m_fileNames[j]);
                    }
                }
            }
        }

        [Test]
        public void TestTrieBuilding()
        {
            Buzzard.Data.classDatasetTrie trie = new Buzzard.Data.classDatasetTrie();
            foreach (classDMSData datum in m_datasets)
            {
                trie.AddData(datum);
            }

            foreach (string name in m_valids.Values)
            {
                classDMSData datum  = null;
                string nname        = Path.GetFileNameWithoutExtension(name);
                try
                {

                    datum = trie.FindData(nname);
                    if (datum != null)
                    {
                        int xx = 0;
                        xx++;
                    }
                }
                catch(KeyNotFoundException)
                {
                    try
                    {
                        string xname    = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(name));
                        datum           = trie.FindData(xname);
                    }
                    catch (KeyNotFoundException)
                    {
                        int xx = 0;
                        xx++;
                    }
                }
            }
        }
    }
}
