using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichIO;
using System.Security.Cryptography;

namespace RichIOTest
{
    [TestClass]
    public class RichIOTest
    {
        private RichIO.RichIO rio;
        private SHA512 sha2;

        public RichIOTest()
        {
            rio = new RichIO.RichIO("database.db", "storage.sto");
        }

        private byte[] ReadBytes(string path)
        {
            byte[] data;
            using (var file = new FileStream(path, FileMode.Open))
            {
                file.Seek(0, SeekOrigin.End);
                var size = (int)file.Position;
                data = new byte[size];
                file.Seek(0, SeekOrigin.Begin);
                file.Read(data, 0, size);
            }
            return data;
        }

        private long GetHashCode(byte[] buffer)
        {
            sha2 = SHA512.Create();
            var hash = sha2.ComputeHash(buffer);
            sha2.Clear();
            return BitConverter.ToInt64(hash);
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
        public void TestImportOne()
        {
            rio.Truncate();

            var buffer = ReadBytes("assets/1.jpg");
            var hash = GetHashCode(buffer);
            var id = rio.Write(buffer);

            var buffer2 = rio.Read(id);
            var hash2 = GetHashCode(buffer2);

            Assert.AreEqual(hash, hash2);
        }

        [TestMethod]
        public void TestImportMore()
        {
        }
    }
}
