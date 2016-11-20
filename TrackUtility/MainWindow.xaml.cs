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
using SRFS.IO;
using SRFS.Model;
using MoreLinq;
using System.Threading;
using System.ComponentModel;

namespace TrackUtility {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
        }

        private void FileSystem_Open_MenuItem_Click(object sender, RoutedEventArgs e) {
            OpenFileSystemWindow w = new OpenFileSystemWindow();
            w.Owner = this;
            bool? result = w.ShowDialog();

            if (result != false) {
                LoadFileSystem(w.FileSystem);
            }
        }

        private void LoadFileSystem(FileSystem fs) {
            _fileSystem = fs;
            viewTracksMenuItem.IsEnabled = true;
        }

        private FileSystem _fileSystem;

        private void View_Tracks_MenuItem_Click(object sender, RoutedEventArgs e) {
            // Need to limit to one!  Maybe add pages to a tab control.
            TrackStatusWindow w = new TrackStatusWindow();
            w.Owner = this;
            w.Open(_fileSystem);
            w.Show();
        }
    }
}
