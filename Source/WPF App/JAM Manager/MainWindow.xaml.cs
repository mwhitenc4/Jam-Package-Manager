using Microsoft.Win32;
using System;
using System.IO;
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
using forms = System.Windows.Forms;

namespace JAM_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static JAM current;
        public MainWindow()
        {
            InitializeComponent();
        }

        class ListItem
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public int FileSize { get; set; }
            public int Offset { get; set; }
            public FileInfo File { get; set; }
        }

        void ReloadFile()
        {
            if (current != null)
            {
                lstAllFiles.Items.Clear();
                current.Files.Sort((x, y) => x.FileName[0] - y.FileName[0]);
                foreach (FileInfo f in current.Files)
                {
                    if (ftrBox.Text == "" || 
                        f.FileName.ToLower().Contains(ftrBox.Text.ToLower()) ||
                        f.FileExtension.ToLower().Contains(ftrBox.Text.ToLower()))
                    {
                        string fullName = f.FileName + "." + f.FileExtension;
                        lstAllFiles.Items.Add(new ListItem { Name = fullName, Extension = f.FileExtension, FileSize = f.FileSize, Offset = f.FileOffset, File = f });
                    }
                }
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog() == true)
            {
                current = JAM.Read(fd.FileName);
                ReloadFile();
            }
        }

        private void lstAllFiles_Drop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            // don't ask, I'm tired now
            if (droppedFiles.Length == 1 && droppedFiles[0].Length >= 4 && droppedFiles[0].Substring(droppedFiles[0].Length - 4, 4).ToUpper() == ".JAM")
            {
                current = JAM.Read(droppedFiles[0]);
                ReloadFile();
                return;
            }

            foreach (string fileName in droppedFiles)
            {
                FileInfo f = current.FindFile(Path.GetFileName(fileName));
                if (f != null)
                {
                    f.ReplaceWithFile(fileName);
                }
            }
            ReloadFile();
            current.Export();
        }

        private void btnExportFile_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListItem f in lstAllFiles.SelectedItems)
            {
                SaveFileDialog save = new SaveFileDialog();
                save.FileName = f.File.FileName + "." + f.File.FileExtension;
                save.DefaultExt = f.File.FileExtension;
                if (save.ShowDialog() == true)
                {
                    f.File.Extract(save.FileName);
                }
            }
            ReloadFile();
        }

        private void ftrBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReloadFile();
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            // What the fuck microsoft
            forms.FolderBrowserDialog fd = new forms.FolderBrowserDialog();
            if (fd.ShowDialog() == forms.DialogResult.OK) {
                foreach (FileInfo f in current.Files)
                {
                    try
                    {
                        f.Extract(fd.SelectedPath + "/" + f.FileName + "." + f.FileExtension);
                    }
                    catch
                    {
                        // I really can't be bothered to find out what this error is, probably relating to JAM folders
                    }
                }
            }
        }
    }
}
