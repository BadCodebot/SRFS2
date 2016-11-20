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

namespace TrackUtility {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MountFileSystemWindow : Window {

        public MountFileSystemWindow() {
            InitializeComponent();
//            DataContext = data;
        }

        private void SaveExecuted(object sender, RoutedEventArgs e) {
            //using (var decryptionPassword = decryptionKeyPasswordBox.SecurePassword)
            //using (var signingPassword = signingKeyPasswordBox.SecurePassword) {
            //    try {
            //        _fileSystem = data.MountFileSystem(decryptionPassword, signingPassword);
            //        if (_fileSystem != null) {
            //            DialogResult = true;
            //        }
            //    } catch (Exception ex) {
            //        MessageBox.Show(ex.Message, "Mount Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        public FileSystem FileSystem => _fileSystem;
        private FileSystem _fileSystem = null;

        private void SelectFolder_Button_Click(object sender, RoutedEventArgs e) {
            //System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            //var result = dlg.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK) {
            //    data.KeyFolderPath = dlg.SelectedPath;
            //}
        }

        private void SelectDecryptionKey_Button_Click(object sender, RoutedEventArgs e) {
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.Title = "Select Decryption Key";
            //dlg.DefaultExt = ".key";
            //dlg.Filter = "Key Files (*.key)|*.key";
            //dlg.Multiselect = false;

            //bool? result = dlg.ShowDialog();
            //if (result == true) {
            //    data.DecryptionKeyPath = dlg.FileName;
            //}
        }

        private void SelectEncryptionKey_Button_Click(object sender, RoutedEventArgs e) {
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.Title = "Select Encryption Key";
            //dlg.DefaultExt = ".key";
            //dlg.Filter = "Key Files (*.key)|*.key";
            //dlg.Multiselect = false;

            //bool? result = dlg.ShowDialog();
            //if (result == true) {
            //    data.EncryptionKeyPath = dlg.FileName;
            //}
        }

        private void SelectSignatureKey_Button_Click(object sender, RoutedEventArgs e) {
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.Title = "Select Signature Key";
            //dlg.DefaultExt = ".key";
            //dlg.Filter = "Key Files (*.key)|*.key";
            //dlg.Multiselect = false;

            //bool? result = dlg.ShowDialog();
            //if (result == true) {
            //    data.SigningKeyPath = dlg.FileName;
            //}
        }

        private void SaveCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            //e.CanExecute = data.Validate();
        }

//        private OpenFileSystemData data = new OpenFileSystemData();
    }
}
