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

        private void ReadBuffer(FileStream dbfs, byte[] buffer, int id)
        {
            dbfs.Seek(id * 8, SeekOrigin.Begin);
            dbfs.ReadAsync(buffer, 0, 8);
        }

        private void ReadImage(FileStream storage, byte[] image, ref FileInfo info)
        {
            storage.Seek(info.Offset, SeekOrigin.Begin);
            storage.ReadAsync(image, 0, (int)info.Size);
        }

        private async Task<byte[]> ReadImageTask(FileStream dbfs, FileStream storage, byte[] buffer, int id)
        {
            Task<byte[]> imageTask = Task.Run<byte[]>(() =>
            {
                ReadBuffer(dbfs, buffer, id);

                FileInfo info = new FileInfo(buffer);
                var image = new byte[info.Size];

                ReadImage(storage, image, ref info);
                return image;
            });
            return await imageTask;
        }
        
        /// <summary>
        /// 非同期で読み込みを行う
        /// </summary>
        /// <param name="id">画像ID</param>
        /// <returns>バイナリ</returns>
        public async Task<byte[]> Read(int id)
        {
            Task<byte[]> task;
            var buffer = new byte[8];
            using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read))
            {
                using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read))
                {
                    task = ReadImageTask(dbfs, storage, buffer, id);
                }
            }
            return await task;
        }

        /// <summary>
        /// 非同期で複数画像を読み込む
        /// </summary>
        /// <param name="ids">複数の画像ID</param>
        /// <returns>画像IDに紐付いた複数のバイナリ</returns>
        public async Task<byte[][]> Read(int[] ids)
        {
            Task<byte[][]> task;
            using (var dbfs = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read))
            {
                using (var storage = new FileStream(StoragePath, FileMode.Open, FileAccess.Read))
                {
                    task = Task.Run<byte[][]>(() =>
                    {
                        var images = new byte[ids.Length][];
                        for (int i = 0; i < images.Length; ++i)
                        {
                            images[i] = ReadImageTask(dbfs, storage, images[i], ids[i]).Result;
                        }
                        return images;
                    });
                }
            }
            return await task;
        }

        /// <summary>
        /// 画像を書き込んで書き込まれたIDを返す
        /// </summary>
        /// <param name="image">画像のバイナリ</param>
        /// <returns>画像ID</returns>
        public int Write(byte[] image)
        {
            int id;
            using (var storage = new FileStream(StoragePath, FileMode.Append | FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                using (var dbfs = new FileStream(DatabasePath, FileMode.Append | FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    int offset = (int)storage.Seek(0, SeekOrigin.End);
                    storage.Write(image, 0, image.Length);

                    dbfs.Seek(0, SeekOrigin.End);
                    id = (int)dbfs.Position / 8;
                    var fileinfo = new FileInfo(image.Length, offset);
                    dbfs.Write(fileinfo.ToBytes(), 0, 8);

                    dbfs.Flush();
                    storage.Flush();
                }
            }
            return id;
        }

        /// <summary>
        /// 非同期で画像の書き込み
        /// </summary>
        /// <param name="images">画像のバイナリの配列</param>
        /// <returns>画像IDの配列</returns>
        public async Task<int[]> Write(byte[][] images)
        {
            Task<int[]> task = Task.Run<int[]>(() =>
            {
                var ids = new int[images.Length];

                using (var storage = new FileStream(StoragePath, FileMode.Append | FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    using (var dbfs = new FileStream(DatabasePath, FileMode.Append | FileMode.Open, FileAccess.Write, FileShare.Read))
                    {
                        // 書き込みバッファの確保
                        int total = 0;
                        int[] offsets = new int[images.Length];
                        offsets[0] = (int)storage.Seek(0, SeekOrigin.End);

                        // オフセットを調べる
                        for (int i = 1; i < images.Length; ++i)
                        {
                            offsets[i] = offsets[i - 1] + images[i - 1].Length;
                        }

                        // データベースに書き込んで全体のサイズを確定する
                        dbfs.Seek(0, SeekOrigin.End);
                        for (int i = 0; i < images.Length; ++i)
                        {
                            var info = new FileInfo(images[i].Length, offsets[i]);
                            dbfs.Write(info.ToBytes(), 0, 8);

                            ids[i] = (int)dbfs.Position / 8 - 1;
                            total += images[i].Length;
                        }

                        // バッファを確保してまとめて書き込む
                        byte[] buffer = new byte[total];
                        int offset = 0;
                        for (int i = 0; i < images.Length; ++i)
                        {
                            Array.Copy(buffer, offset, images[i], 0, images[i].Length);
                            offset += images[i].Length;
                        }
                        storage.Write(buffer, 0, buffer.Length);
                        dbfs.Flush();
                        storage.Flush();
                    }
                }

                return ids;
            });
            
            return await task;
        }

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
