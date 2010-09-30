using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using GitSharp;

namespace GitExplorer
{
    public partial class MainForm : Form
    {
        Repository repo;
        List<Commit> commits;
        Dictionary<string, Commit> commitDict;
        Tree currentTree;
        ListViewGroup folders = new ListViewGroup();
        ListViewGroup files = new ListViewGroup();
        ImageList images = new ImageList();
        Dictionary<Process, string> processes = new Dictionary<Process, string>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        void Initialize()
        {
            images.Images.Add(Image.FromFile("Generic_Document.png"));
            images.Images.Add(Image.FromFile("Stuffed_Folder.png"));
            images.Images.Add(Image.FromFile("FolderOpen_48x48_72.png"));
            images.Images.Add(Image.FromFile("075b_UpFolder_48x48_72.png"));
            images.Images.Add(Image.FromFile("generic_picture.png"));
            images.ImageSize = new Size(48, 48);
            listView1.LargeImageList = images;
            folderBrowserDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(folderBrowserDialog1.SelectedPath))
            {
                repo = new Repository(folderBrowserDialog1.SelectedPath);
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                commits = new List<Commit>();
                foreach (Commit c in repo.Head.CurrentCommit.Ancestors)
                    commits.Add(c);

                commitDict = new Dictionary<string, Commit>();
                comboBox1.Items.Clear();
                foreach (Commit c in commits)
                {
                    string s = c.CommitDate + " - " + c.Message;
                    comboBox1.Items.Add(s);
                    commitDict.Add(s, c);
                }

                comboBox1.SelectedValueChanged += new EventHandler(ChangeCommit);
                listView1.ItemActivate += new EventHandler(OnItemClicked);

                comboBox1.SelectedIndex = 0;
                LoadTree(commits[0].Tree);
            }
            this.WindowState = FormWindowState.Normal;
        }

        void OnItemClicked(object sender, EventArgs e)
        {
            if (listView1.SelectedItems[0].Group == files)
            {
                foreach (Leaf leaf in currentTree.Leaves)
                {
                    if (leaf.Name == listView1.SelectedItems[0].Text)
                    {
                        var data = leaf.RawData;
                        var splitName = leaf.Name.Split(".".ToCharArray(0, 1), 2, StringSplitOptions.None);
                        var tempFileName = Path.GetTempPath() + splitName[0] + "_" + leaf.ShortHash + "." + splitName[1];
                        using (var w = new BinaryWriter(new FileStream(tempFileName, FileMode.Create)))
                            w.Write(data);
                        var process = Process.Start(tempFileName);
                        if (process != null)
                        {
                            process.Exited += new EventHandler(CheckProcesses);
                            processes.Add(process, tempFileName);
                        }
                    }
                }
            }
            else if (listView1.SelectedItems[0].Group == folders)
            {
                foreach (Tree t in currentTree.Trees)
                {
                    if (t.Name == listView1.SelectedItems[0].Text)
                        LoadTree(t);
                }
            }
            else if (listView1.SelectedItems[0].Text == "...")
            {
                LoadTree(currentTree.Parent);
            }
        }

        void ChangeCommit(object sender, EventArgs e)
        {
            var commit = commitDict[comboBox1.SelectedItem as string];
            LoadTree(commit.Tree);
        }

        List<string> imageExtensions = new List<string>(new string[] { "png", "jpg", "jpeg", "bmp", "tiff" });

        public void LoadTree(Tree tree)
        {
            listView1.Clear();

            if (!tree.IsRoot)
                listView1.Items.Add(new ListViewItem("...", 3));

            foreach (Tree t in tree.Trees)
            {
                var leaves = 0;
                foreach (Leaf l in t.Leaves)
                    leaves++;
                if (leaves > 0)
                    listView1.Items.Add(new ListViewItem(t.Name, 1, folders));
                else
                    listView1.Items.Add(new ListViewItem(t.Name, 2, folders));
            }

            foreach (Leaf l in tree.Leaves)
            {
                string[] splitFilename = l.Name.Split('.');
                string extension = splitFilename[splitFilename.GetLength(0) - 1];
                if (imageExtensions.Contains(extension))
                    listView1.Items.Add(new ListViewItem(l.Name, 4, files));
                else
                    listView1.Items.Add(new ListViewItem(l.Name, 0, files));
            }

            currentTree = tree;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Initialize();
        }

        void CheckProcesses(object sender, EventArgs e)
        {
            foreach (Process p in processes.Keys)
            {
                if (p.HasExited)
                    File.Delete(processes[p]);
            }
        }
    }
}
