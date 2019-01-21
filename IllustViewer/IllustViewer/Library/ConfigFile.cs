using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Windows;

namespace IllustViewer.Library
{
    public class Config
    {
        public string ConfigPath;
        public string DatabasePath;
        public string StoragePath;
        public string SQLitePath;
    }

    public class ConfigFile
    {
        XmlSerializer serializer;
        public string ConfigPath { get; private set; }
        public Config Config { get; set; }

        /// <summary>
        /// 設定ファイルクラス
        /// </summary>
        /// <param name="configPath"></param>
        public ConfigFile(string configPath)
        {
            ConfigPath = configPath;
            serializer = new XmlSerializer(typeof(Config));

            using (var stream = new StreamReader(ConfigPath, new System.Text.UTF8Encoding(true)))
            {
                Config = serializer.Deserialize(stream) as Config;
            }
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        public void Save()
        {
            using (var stream = new StreamWriter(ConfigPath, false, new System.Text.UTF8Encoding(true)))
            {
                serializer.Serialize(stream, Config);
            }
        }

        /// <summary>
        /// 設定ファイルを移動する
        /// </summary>
        /// <param name="moveTo"></param>
        public void Move(string moveTo)
        {
            try
            {
                File.Move(ConfigPath, moveTo);
                ConfigPath = moveTo;
            }
            catch
            {
                MessageBox.Show("ファイルの移動に失敗したのでコピーを作成します", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                try
                {
                    File.Copy(ConfigPath, moveTo);
                    ConfigPath = moveTo;
                }
                catch
                {
                    MessageBox.Show("ファイルを移動できませんでした", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
