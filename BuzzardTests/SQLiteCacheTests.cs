﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.IO.SQLite;
using NUnit.Framework;

namespace BuzzardTests
{
    [TestFixture]
    public class SQLiteCacheTests
    {
        const string CONST_TEST_FOLDER = "LCMSNetUnitTests";
        const string CONST_TEST_CACHE = "SQLiteToolsUnitTests.que";

        private const bool DELETE_CACHE_DB = true;

        public SQLiteCacheTests()
        {
            SQLiteTools.Initialize(CONST_TEST_FOLDER);

            var appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(appPath, CONST_TEST_FOLDER, CONST_TEST_CACHE);
            if (File.Exists(file) && DELETE_CACHE_DB)
            {
                File.Delete(file);
            }

            // Note that this will call BuildConnectionString
            SQLiteTools.SetCacheLocation(file);
        }

        /**
         * Tests are named TestA-TestZ  to get NUnit to execute them in order
         */

        /// <summary>
        /// tests that the connection string being used by SQLiteTools is correct.
        /// </summary>
        [Test]
        public void TestA()
        {
            // The actual build connection string call is in constructor
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                          CONST_TEST_FOLDER, CONST_TEST_CACHE);
            Console.WriteLine("ConnectionString: " + SQLiteTools.ConnString);
            Assert.AreEqual("data source=" + folderPath, SQLiteTools.ConnString);
        }

        /// <summary>
        /// Tests that SaveSingleColumnListToCache works
        /// </summary>
        [Test]
        public void TestB()
        {
            var testSeparationTypes = new List<string>
            {
                "Separation 1",
                "Separation 2",
                "Separation 3"
            };

            // if the following line doesn't throw an exception, it "worked".
            SQLiteTools.SaveSeparationTypeListToCache(testSeparationTypes);
        }

        /// <summary>
        /// Tests that GetSepTypeList gets separations, specifically those stored by TestB.
        /// </summary>
        [Test]
        public void TestC()
        {
            var testList = new List<string>
            {
                "Separation 1",
                "Separation 2",
                "Separation 3"
            };
            var retrieved = SQLiteTools.GetSepTypeList(false);
            Assert.IsTrue(retrieved.SequenceEqual(testList)); // If this is equal, both TestB and C worked, and we read the information back from the cache
        }

        /// <summary>
        /// Checks that SaveUserListToCache works(or thinks it does)
        /// </summary>
        [Test]
        public void TestD()
        {
            var usersExampleData = new List<UserInfo>();
            var example = new UserInfo
            {
                Id = "1",
                Name = "Test User"
            };
            usersExampleData.Add(example);
            SQLiteTools.SaveUserListToCache(usersExampleData);
        }

        /// <summary>
        /// Tests that GetUserList returns correct users. Specifically the one added by TestD.
        /// </summary>
        [Test]
        public void TestE()
        {
            var users = SQLiteTools.GetUserList(false).ToList();
            Assert.AreEqual(1, users.Count);
            Assert.IsTrue(users.Exists(x => x.Name == "Test User" && x.Id == "1"));
        }

        /// <summary>
        /// Test that experiments are saved to cache correctly
        /// </summary>
        [Test]
        public void TestL()
        {
            var experiments = new List<ExperimentData>();
            var experiment = new ExperimentData
            {
                ID = 1,
                Experiment = "Test",
                Created = DateTime.Now,
                Researcher = "Staff",
                Reason = "Software testing"
            };
            experiments.Add(experiment);
            SQLiteTools.SaveExperimentListToCache(experiments);
        }

        /// <summary>
        /// Tests that experiments are read from cache
        /// </summary>
        [Test]
        public void TestM()
        {
            var experiments = SQLiteTools.GetExperimentList().ToList();
            Assert.AreEqual(1, experiments[0].ID);
            Assert.AreEqual("Test", experiments[0].Experiment);
        }

        /// <summary>
        /// Test that instruments are saved to cache correctly
        /// </summary>
        [Test]
        public void TestN()
        {
            var instInfo = new List<InstrumentInfo>();
            var inst = new InstrumentInfo
            {
                CommonName = "Test instrument"
            };
            instInfo.Add(inst);
            SQLiteTools.SaveInstListToCache(instInfo);
        }

        /// <summary>
        /// Tests that instruments are read from cache
        /// </summary>
        [Test]
        public void TestO()
        {
            var instruments = SQLiteTools.GetInstrumentList(false).ToList();
            Assert.AreEqual("Test instrument", instruments[0].CommonName);
        }

        /// <summary>
        /// Test that proposal users are saved to cache correctly
        /// </summary>
        [Test]
        public void TestP()
        {
            var users = new List<ProposalUser>();
            var user = new ProposalUser
            {
                UserID = 1
            };
            users.Add(user);
            SQLiteTools.SaveProposalUsers(users, new List<UserIDPIDCrossReferenceEntry>(), new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>());
        }

        /// <summary>
        /// Tests that proposal users are read from cache
        /// </summary>
        [Test]
        public void TestQ()
        {
            SQLiteTools.GetProposalUsers(out var users, out _);
            Assert.AreEqual(1, users[0].UserID);
        }

        /// <summary>
        /// Tests that LCColumns are saved to cache
        /// </summary>
        [Test]
        public void TestR()
        {
            var cols = new List<string>
            {
                "ColTest1"
            };

            SQLiteTools.SaveColumnListToCache(cols);
        }

        /// <summary>
        /// Tests that LCColumns are read from cache
        /// </summary>
        [Test]
        public void TestS()
        {
            var cols = SQLiteTools.GetColumnList(false).ToList();
            Assert.AreEqual(1, cols.Count);
            Assert.IsTrue(cols[0] == "ColTest1");
        }
    }
}
