using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RichIO
{
    public struct FileInfo
    {
        public int Size { get; private set; }
        public int Offset { get; private set; }

        public FileInfo(byte[] chunk)
        {
            Size = BitConverter.ToInt32(chunk, 0);
            Offset = BitConverter.ToInt32(chunk, 4);
        }

        public FileInfo(int size, int offset)
        {
            Size = size;
            Offset = offset;
        }

        public byte[] ToBytes()
        {
            var buffer = new byte[8];
            Buffer.BlockCopy(buffer, 0, BitConverter.GetBytes(Size), 0, 4);
            Buffer.BlockCopy(buffer, 4, BitConverter.GetBytes(Offset), 0, 4);
            return buffer;
        }
    }

    public class RichIO
    {
        public string DatabasePath { get; private set; }
        public string StoragePath { get; private set; }

        long fileSize;

        public RichIO(string databasePath, string storagePath)
        {
            DatabasePath = databasePath;
            StoragePath = storagePath;
        }

        /// <summary>
        /// 非同期で読み込みを行う
        /// </summary>
        /// <param name="id">画像ID</param>
        /// <returns>バイナリ</returns>
        public async Task<byte[]> Read(int id)
        {
            var buffer = new byte[8];

            Task<byte[]> imageTask = Task.Run<byte[]>(() => {
                using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read))
                {
                    dbfs.Seek(id * 8, SeekOrigin.Begin);
                    dbfs.ReadAsync(buffer, 0, 8);
                }

                FileInfo info = new FileInfo(buffer);
                var image = new byte[info.Size];

                using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read))
                {
                    storage.Seek(info.Offset, SeekOrigin.Begin);
                    storage.ReadAsync(image, 0, (int)info.Size);
                }
                return image;
            });

            return await imageTask;
        }

        /// <summary>
        /// 非同期で複数画像を読み込む
        /// </summary>
        /// <param name="ids">複数の画像ID</param>
        /// <returns>画像IDに紐付いた複数のバイナリ</returns>
        public async Task<byte[][]> Read(int[] ids)
        {
            Task<byte[][]> imagesTask = Task.Run<byte[][]>(() =>
            {
                var images = new byte[ids.Length][];

                for (int i = 0; i < images.Length; ++i)
                {
                    images[i] = Read(ids[i]).Result;
                }
                return images;
            });
            
            return await imagesTask;
        }

        /// <summary>
        /// 画像を書き込んで書き込まれたIDを返す
        /// </summary>
        /// <param name="image">画像のバイナリ</param>
        /// <returns>画像ID</returns>
        public int Write(byte[] image)
        {
            int id = -1;
            int size = image.Length;
            int offset = -1;

            using (var storage = new FileStream(StoragePath, FileMode.Append | FileMode.Open, FileAccess.Write))
            {
                offset = (int)storage.Seek(0, SeekOrigin.End);
                storage.Write(image, 0, image.Length);
            }

            using (var dbfs = new FileStream(DatabasePath, FileMode.Append | FileMode.Open, FileAccess.Write))
            {
                dbfs.Seek(0, SeekOrigin.End);
                id = (int)dbfs.Position / 8;
                var fileinfo = new FileInfo(size, offset);
                dbfs.Write(fileinfo.ToBytes(), 0, 8);
            }

            return id;
        }

        /// <summary>
        /// 画像の書き込み
        /// </summary>
        /// <param name="images">画像のバイナリの配列</param>
        /// <returns>画像IDの配列</returns>
        public int[] Write(byte[][] images)
        {
            var ids = new int[images.Length];

            for (int i = 0; i < images.Length; ++i)
            {
                ids[i] = Write(images[i]);
            }

            return ids;
        }

        public void Truncate()
        {
            var dbfs = new FileStream(DatabasePath, FileMode.Truncate | FileMode.Create, FileAccess.Write);
            var storage = new FileStream(StoragePath, FileMode.Truncate | FileMode.Create, FileAccess.Write);
        }
    }
}
