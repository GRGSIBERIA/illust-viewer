using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace IllustViewer.Library
{
    public class WatchDog : IDisposable
    {
        private Window window;
        private RichIO.RichIO rio;
        private List<FileSystemWatcher> dogs;
        
        private const string filter = "*.jpg|*.jpeg|*.png|*.large_jpg|*.large_jpeg|*.large_png";

        private void AddDog(string path)
        {
            if (!Directory.Exists(path))
                throw new FileNotFoundException(path);

            // 既にディレクトリが存在する場合はイベントの重複を防ぐため操作を無視する
            foreach (var d in dogs)
            {
                if (d.Path == path)
                    return;
            }

            var dog = new FileSystemWatcher(path, filter);
            dog.IncludeSubdirectories = true;
            dog.EnableRaisingEvents = true;
            dog.Created += AddFile;
            dogs.Add(dog);
        }

        public WatchDog(Window window, RichIO.RichIO rio, string[] watchPathes)
        {
            this.window = window;
            this.rio = rio;
            dogs = new List<FileSystemWatcher>();

            foreach (var path in watchPathes)
                AddDog(path);
        }

        /// <summary>
        /// ディレクトリを追加する
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="FileNotFoundException">ファイルが存在しない</exception>
        public void AddDirectory(string path)
        {
            AddDog(path);
        }

        /// <summary>
        /// 監視対象のディレクトリを削除する
        /// 存在しなければ操作を無視する
        /// </summary>
        /// <param name="path"></param>
        public void RemoveDirectory(string path)
        {
            FileSystemWatcher removal = null;
            foreach (var dog in dogs)
            {
                if (dog.Path == path)
                {
                    removal = dog;
                }
            }
            if (removal != null)
                dogs.Remove(removal);
        }

        /// <summary>
        /// 画像のバイナリを読み込む
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns>バイナリ</returns>
        private byte[] LoadImage(string path)
        {
            byte[] result = null;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(0, SeekOrigin.End);
                var size = (int)stream.Position;
                result = new byte[size];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(result, 0, size);
            }
            return result;
        }

        /// <summary>
        /// ファイルが追加されたときのイベント
        /// </summary>
        /// <remarks>追加されたファイルのIDを通知する方法がわからない</remarks>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void AddFile(Object source, FileSystemEventArgs e)
        {
            // 非同期で呼び出すのにWindowが必要
            window.Dispatcher.Invoke((Action)(()=>
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        rio.Write(LoadImage(e.FullPath));
                        break;
                    default:
                        break;
                }
            }));
        }

        public void Dispose()
        {
            for (int i = 0; i < dogs.Count; ++i)
            {
                dogs[i].Dispose();
                dogs[i] = null;
            }
        }
    }
}
