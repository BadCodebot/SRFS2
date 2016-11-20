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
using System.Windows.Shapes;
using SRFS.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.InteropServices;
using SRFS.Model;
using TrackUtility.Model;

namespace TrackUtility.Dialogs {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class OpenFileSystemWindow : Window {

        public OpenFileSystemWindow() {
            InitializeComponent();
            DataContext = data;
        }

        private void SaveExecuted(object sender, RoutedEventArgs e) {
            using (var decryptionPassword = decryptionKeyPasswordBox.SecurePassword) {
                try {
                    _fileSystem = data.OpenFileSystem(decryptionPassword);
                    if (_fileSystem != null) {
                        DialogResult = true;
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Mount Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public FileSystem FileSystem => _fileSystem;
        private FileSystem _fileSystem = null;

        private void SelectFolder_Button_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                data.KeyFolderPath = dlg.SelectedPath;
            }
        }

        private void SelectDecryptionKey_Button_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Select Decryption Key";
            dlg.DefaultExt = ".key";
            dlg.Filter = "Key Files (*.key)|*.key";
            dlg.Multiselect = false;

            bool? result = dlg.ShowDialog();
            if (result == true) {
                data.DecryptionKeyPath = dlg.FileName;
            }
        }

        private void SaveCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = data.Validate();
        }

        private OpenFileSystemWindowModel data = new OpenFileSystemWindowModel();
    }
}
