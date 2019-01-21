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

        private void CompareHash(byte[] a, byte[] b)
        {
            var hash1 = GetHashCode(a);
            var hash2 = GetHashCode(b);
            Assert.AreEqual(hash1, hash2);
        }

        private void AssertWriteAsRead(int i)
        {
            string path = "assets/" + i.ToString() + ".jpg";

            var buffer = ReadBytes(path);
            var id = rio.Write(buffer);

            var buffer2 = rio.Read(id);
            CompareHash(buffer, buffer2);
        }

        private void AssertRead(int id)
        {
            string path = "assets/" + (id + 1).ToString() + ".jpg";

            var buffer = ReadBytes(path);
            var buffer2 = rio.Read(id);
            CompareHash(buffer, buffer2);
        }

        [TestMethod]
        public void TestImportOne()
        {
            rio.Truncate();

            AssertWriteAsRead(1);
        }

        [TestMethod]
        public void TestImportMore()
        {
            rio.Truncate();

            for (int i = 1; i <= 23; ++i)
            {
                AssertWriteAsRead(i);
            }
        }

        [TestMethod]
        public void RandomRead()
        {
            rio.Truncate();
            List<int> random = new List<int>();
            System.Random rnd = new Random();

            for (int i = 1; i < 24; ++i)
            {
                AssertWriteAsRead(i);
                random.Add(i);
            }

            var randomsort = random.OrderBy(i => Guid.NewGuid()).ToArray();

            foreach (var i in randomsort)
            {
                AssertRead(i - 1);
            }
        }

        private int[] WriteList()
        {
            byte[][] images = new byte[23][];
            for (int i = 1; i < 24; ++i)
            {
                string path = "assets/" + i.ToString() + ".jpg";
                images[i - 1] = ReadBytes(path);
            }

            return rio.Write(images);
        }

        [TestMethod]
        public void TestImportList()
        {
            rio.Truncate();
            int[] ids = WriteList();
            
            foreach (var id in ids)
            {
                AssertRead(id);
            }
        }

        [TestMethod]
        public void TestReadMany()
        {
            rio.Truncate();
            int[] ids = WriteList();
            byte[][] images = rio.Read(ids);

            for (int i = 0; i < images.Length; ++i)
            {
                var buffer = ReadBytes("assets/" + (i + 1).ToString() + ".jpg");
                CompareHash(images[i], buffer);
            }
        }
    }
}
