using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using IllustViewer.Library;

namespace IllustViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        WatchDog watchdog;
        RichIO.RichIO richio;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (sender, e) => {
                // 実際にはコンフィグファイルを読み込んだ後に処理する必要があるので
                // コンフィグを読むまではここで仮埋めしておく
                // コンフィグが実装できたらきちんと埋める
                richio = new RichIO.RichIO("database.db", "storage.sto");
                watchdog = new WatchDog(this, richio, new string[] { });
            };
        }
    }
}
