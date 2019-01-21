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
            var sizebuf = BitConverter.GetBytes(Size);
            var offsetbuf = BitConverter.GetBytes(Offset);
            Buffer.BlockCopy(sizebuf, 0, buffer, 0, 4);
            Buffer.BlockCopy(offsetbuf, 0, buffer, 4, 4);
            return buffer;
        }
    }

    public class RichIO
    {
        public string DatabasePath { get; private set; }
        public string StoragePath { get; private set; }

        /// <summary>
        /// データベースの容量を返す
        /// </summary>
        public int DatabaseSize
        {
            get
            {
                int retval;
                using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read))
                {
                    dbfs.Seek(0, SeekOrigin.End);
                    retval = (int)dbfs.Position;
                }
                return retval;
            }
        }

        /// <summary>
        /// ストレージの容量を返す
        /// </summary>
        public int StorageSize
        {
            get
            {
                int retval;
                using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read))
                {
                    storage.Seek(0, SeekOrigin.End);
                    retval = (int)storage.Position;
                }
                return retval;
            }
        }

        public RichIO(string databasePath, string storagePath)
        {
            DatabasePath = databasePath;
            StoragePath = storagePath;

            // ファイルが存在しない場合は作成する
            using (var dbfs = new FileStream(databasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) { }
            using (var storage = new FileStream(storagePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) { }
        }

        private FileInfo ReadFileInfo(FileStream dbfs, int id)
        {
            var buffer = new byte[8];
            dbfs.Seek(id * 8, SeekOrigin.Begin);
            dbfs.Read(buffer, 0, 8);
            return new FileInfo(buffer);
        }

        private byte[] ReadImage(ref FileInfo info, FileStream storage)
        {
            var image = new byte[info.Size];
            storage.Seek(info.Offset, SeekOrigin.Begin);
            storage.Read(image, 0, info.Size);
            return image;
        }

        /// <summary>
        /// 画像の読み込みを行う
        /// </summary>
        /// <param name="id">画像ID</param>
        /// <returns>バイナリ</returns>
        public byte[] Read(int id)
        {
            using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8))
            {
                var info = ReadFileInfo(dbfs, id);

                using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read, info.Size))
                {
                    return ReadImage(ref info, storage);
                }
            }
        }

        /// <summary>
        /// 複数画像を読み込む
        /// </summary>
        /// <param name="ids">複数の画像ID，ソートしたほうがいい</param>
        /// <returns>画像IDに紐付いた複数のバイナリ</returns>
        public byte[][] Read(int[] ids)
        {
            byte[][] images = new byte[ids.Length][];
            FileInfo[] infos = new FileInfo[ids.Length];

            using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8 * ids.Length))
            {
                int totalSize = 0;
                for (int i = 0; i < images.Length; ++i)
                {
                    infos[i] = ReadFileInfo(dbfs, i);
                    totalSize += infos[i].Size;
                }

                for (int i = 0; i < images.Length; ++i)
                { 
                    using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read, totalSize))
                    {
                        images[i] = ReadImage(ref infos[i], storage);
                    }
                }
            }
            return images;
        }

        private int WriteImage(byte[] image, FileStream dbfs, FileStream storage)
        {
            var storageOffset = (int)storage.Seek(0, SeekOrigin.End);
            storage.Write(image, 0, image.Length);

            var fileinfo = new FileInfo(image.Length, storageOffset);
            int dbfsOffset = (int)dbfs.Seek(0, SeekOrigin.End);
            var id = dbfsOffset / 8;

            dbfs.Write(fileinfo.ToBytes(), 0, 8);

            return id;
        }

        /// <summary>
        /// 画像を書き込んで書き込まれたIDを返す
        /// </summary>
        /// <param name="image">画像のバイナリ</param>
        /// <returns>画像ID</returns>
        public int Write(byte[] image)
        {
            int id;

            using (var storage = new FileStream(StoragePath, FileMode.Append, FileAccess.Write, FileShare.Write, image.Length))
            {
                using (var dbfs = new FileStream(DatabasePath, FileMode.Append, FileAccess.Write, FileShare.Write, 8))
                {
                    id = WriteImage(image, dbfs, storage);
                    dbfs.Flush();
                    storage.Flush();
                }
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
            int[] ids = new int[images.Length];
            int totalSize = 0;
            foreach (var image in images)
                totalSize += image.Length;

            using (var storage = new FileStream(StoragePath, FileMode.Append, FileAccess.Write, FileShare.Write, totalSize))
            {
                using (var dbfs = new FileStream(DatabasePath, FileMode.Append, FileAccess.Write, FileShare.Write, 8 * images.Length))
                {
                    for (int i = 0; i < images.Length; ++i)
                    {
                        ids[i] = WriteImage(images[i], dbfs, storage);
                    }
                    dbfs.Flush();
                    storage.Flush();
                }
            }
            return ids;
        }

        /// <summary>
        /// ファイルが存在しなければ作成して，存在すれば空にする
        /// </summary>
        /// <param name="path"></param>
        private void ExistsAsTruncate(string path)
        {
            if (!File.Exists(path))
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write)) { }
            else
                using (var file = new FileStream(path, FileMode.Truncate, FileAccess.Write)) { }
        }

        /// <summary>
        /// [非推奨] ファイルの中身を空にする
        /// </summary>
        public void Truncate()
        {
            ExistsAsTruncate(DatabasePath);
            ExistsAsTruncate(StoragePath);
        }
    }
}
