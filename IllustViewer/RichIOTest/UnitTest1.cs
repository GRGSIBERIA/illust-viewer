using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichIO;

namespace RichIOTest
{
    [TestClass]
    public class RichIOTest
    {
        private RichIO.RichIO rio;

        public RichIOTest()
        {
            rio = new RichIO.RichIO("database.db", "storage.sto");
        }

        [TestMethod]
        public void TestOpen()
        {
            var rio = new RichIO.RichIO("database.db", "storage.sto");
            Assert.IsNotNull(rio);
        }

        [TestMethod]
        public void TestTruncate()
        {
            rio.Truncate();
            Assert.IsTrue(rio.StorageSize == 0);
            Assert.IsTrue(rio.DatabaseSize == 0);

            File.Delete("database.db");
            File.Delete("storage.sto");

            rio.Truncate();
            Assert.IsTrue(rio.StorageSize == 0);
            Assert.IsTrue(rio.DatabaseSize == 0);
        }

        [TestMethod]
        public void TestReadOne()
        {
        }

        [TestMethod]
        public void TestReadMore()
        {
        }

        [TestMethod]
        public void TestWriteOne()
        {
        }

        [TestMethod]
        public void TestWriteMore()
        {
        }
    }
}
