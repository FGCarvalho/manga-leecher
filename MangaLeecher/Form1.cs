using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;
using System.Drawing.Imaging;

namespace MangaLeecher
{
    public partial class frmMain : Form
    {
        #region Properties

        List<Manga> mangas = new List<Manga>();

        static TextWriter twWaitingToBeFetched;
        static Regex regex = new Regex("[^a-zA-Z0-9]");


        #endregion

        #region Constructor

        public frmMain()
        {
            InitializeComponent();

            twWaitingToBeFetched = TextWriter.Synchronized(new StreamWriter(ConfigurationManager.AppSettings["rootDirectory"] + "logErro.txt", true));
        }

        #endregion

        #region Events

        private void tvMangas_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.ByKeyboard || e.Action == TreeViewAction.ByMouse)
            {
                if (e.Node.Checked)
                {
                    if (e.Node.Nodes.Count > 0)
                    {
                        for (var i = 0; i < e.Node.Nodes.Count; i++)
                        {
                            e.Node.Nodes[i].Checked = true;
                        }
                    }
                    else
                    {
                        if (e.Node.Parent != null)
                        {
                            e.Node.Parent.Checked = true;

                            for (var i = 0; i < e.Node.Parent.Nodes.Count; i++)
                            {
                                if (!e.Node.Parent.Nodes[i].Checked)
                                {
                                    e.Node.Parent.Checked = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (e.Node.Nodes.Count > 0)
                    {
                        for (var i = 0; i < e.Node.Nodes.Count; i++)
                        {
                            e.Node.Nodes[i].Checked = false;
                        }
                    }
                    else
                    {
                        if (e.Node.Parent != null)
                            e.Node.Parent.Checked = false;
                    }
                }
            }
        }

        private void tsmiDownload_Click(object sender, EventArgs e)
        {
            tsslProgresso.Text = "Fetching all information needed to start.";

            this.DownloadAllCheckedChaptersInformation();

            tsslProgresso.Text = "Fetching done, chapters download will begin.";

            this.DownloadAllCheckedChapters();
        }

        private void tvMangas_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //this.LoadMangaChapters(e.Node.Index);
            ThreadPool.QueueUserWorkItem(ThreadLoadMangaChapters, new ManualResetEventHelper(new ManualResetEvent(false),e.Node.Index));
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            this.tvMangas.Height = this.Height - 89;
            this.tvMangas.Width = (this.Width / 3);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Aplicativo destinado a fácil leitura de mangas\r\nv0.01");
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {

            this.tsmiStart.Enabled = false;
            this.LoadMangaList();
            tsmiDownload.Enabled = true;
        }

        #endregion

        #region Methods

        public void LoadMangaList()
        {
            mangas.Clear();
            tsslProgresso.Text = "Fetching page...";
            string html = Utils.requestFullStringPage(ConfigurationManager.AppSettings["listPage"].ToString());
            tsslProgresso.Text = "Loading first series of manga...";
            string sCol01 = html.Substring(html.IndexOf("<div class=\"series_col\">"), html.LastIndexOf("<div class=\"series_col\">")-html.IndexOf("<div class=\"series_col\">"));
            tsslProgresso.Text = "Loading second series of manga...";
            string sCol02 = html.Substring(html.LastIndexOf("<div class=\"series_col\">"), html.IndexOf("<div id=\"adfooter\">") - html.LastIndexOf("<div class=\"series_col\">"));

            mangas.AddRange(Manga.generateList(Regex.Matches(sCol01, "<li>(.*?)</li>", RegexOptions.IgnoreCase)));
            mangas.AddRange(Manga.generateList(Regex.Matches(sCol02, "<li>(.*?)</li>", RegexOptions.IgnoreCase)));

            tvMangas.Nodes.Clear();
            tsslProgresso.Text = "Populating tree of mangas...";
            tvMangas.Nodes.AddRange(Manga.generateTree(mangas).ToArray());
            tsslProgresso.Text = mangas.Count + " mangas loaded";
        }

        private void DownloadAllCheckedChaptersInformation()
        {
            tsslProgresso.Text = "Collect all chapter's to download.";

            List<ManualResetEventHelper> lst = new List<ManualResetEventHelper>();

            for (var i = 0; i < tvMangas.Nodes.Count; i++)
            {
                if (tvMangas.Nodes[i].Checked)
                {
                    if (tvMangas.Nodes[i].Nodes.Count == 0)
                    {
                        lst.Add(new ManualResetEventHelper(new ManualResetEvent(false), i));

                        ThreadPool.QueueUserWorkItem(ThreadLoadMangaChapters, lst.Last());
                    }
                }
            }

            var events = new ManualResetEvent[lst.Count];

            foreach (ManualResetEventHelper m in lst)
                m.Mre.WaitOne();
        }

        private void DownloadAllCheckedChapters()
        {
            List<ManualResetEventHelper> lst = new List<ManualResetEventHelper>();

            List<Chapter> chapters = new List<Chapter>();

            for (var i = 0; i < tvMangas.Nodes.Count; i++)
            {
                if (tvMangas.Nodes[i].Checked)
                {
                    //document.getElementById('img').src
                    //var i = []; var j = document.getElementById('pageMenu').options; for( var k = 0; k < j.length ; k++ ){i.push(j[k].value)}; i;
                    chapters.AddRange(mangas[i].Chapters);
                }
                else
                {
                    if (tvMangas.Nodes[i].Nodes.Count > 0)
                        for (var j = 0; j < tvMangas.Nodes[i].Nodes.Count; j++)
                        {
                            if (tvMangas.Nodes[i].Nodes[j].Checked)
                            {
                                chapters.Add(mangas[i].Chapters[j]);
                            }
                        }
                }
            }

            chapters.ForEach(x => lst.Add(new ManualResetEventHelper(new ManualResetEvent(false), x)));

            lst.ForEach(x => ThreadPool.QueueUserWorkItem(ThreadDownloadChapter, x));

            lst.ForEach(x => x.Mre.WaitOne());

            tsslProgresso.Text = "All chosen Chapter's are done.";

        }

        #endregion
               
        #region thread methods

        public void ThreadDownloadChapter(object obj)
        {
            var helper = (ManualResetEventHelper)obj;
            var chapter = (Chapter)helper.ObjetoAuxiliar;

            tsslProgresso.Text = chapter.Title + " Downloading";

            string html = Utils.requestFullStringPage(chapter.Url);

            List<Uri> uris = new List<Uri>();

            var images = html.Substring(html.IndexOf("<select id=\"pageMenu\""));

            images = images.Substring(0, images.IndexOf("</select>"));

            while (images.IndexOf("value") > -1)
            {
                images = images.Substring(images.IndexOf("value=\"") + 7);

                uris.Add(new Uri(ConfigurationManager.AppSettings["rootPage"].ToString() + images.Substring(0, images.IndexOf("\""))));
            }
            
            for(var i = 0; i < uris.Count ; i++){
                DownloadImage(uris[i], 
                    chapter.MangaTitle,
                    chapter.Title,
                    i);
            }

            tsslProgresso.Text = chapter.Title + " Downloaded with " + uris.Count + " pages.";

            helper.Mre.Set();
        }

        private static void DownloadImage(Uri u, String manga, String chap, int image, int tries = 0 )
        {
            try
            {
                Directory.CreateDirectory(regex.Replace((ConfigurationManager.AppSettings["rootDirectory"] + manga + "\\" + chap), String.Empty));

                string html = Utils.requestFullStringPage(u.AbsoluteUri);

                var url = html.Substring(html.IndexOf("<img id=\"img\""));
                url = url.Substring(url.IndexOf("src=\"") + 5);
                url = url.Substring(0, url.IndexOf("\""));

                using (WebClient webClient = new WebClient())
                {
                    byte[] data = webClient.DownloadData(url);

                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        var yourImage = Image.FromStream(mem);

                        yourImage.Save(ConfigurationManager.AppSettings["rootDirectory"] + manga + "\\" + chap + "\\" + image + "." + ImageFormat.Jpeg, ImageFormat.Jpeg);
                    }

                }
            }
            catch (Exception exp)
            {
                if (tries > 50)
                {
                    twWaitingToBeFetched.WriteLine(DateTime.Now.ToString("yyyy-MM-dd") + "[ERROR] " + u);
                    twWaitingToBeFetched.WriteLine(DateTime.Now.ToString("yyyy-MM-dd") + "[ERROR] " + exp.Message);
                    twWaitingToBeFetched.Flush();
                }
                else
                {
                    DownloadImage(u, manga, chap, image, ++tries);
                }
            }
        }

        public void ThreadLoadMangaChapters(object obj)
        {
            var helper = (ManualResetEventHelper)obj;

            var index = Int32.Parse(helper.ObjetoAuxiliar.ToString());

            string html = Utils.requestFullStringPage(mangas[index].Url);

            var mangaTitle = html.Substring(html.IndexOf("<td class=\"propertytitle\">Name:</td>")+60);
            mangaTitle = mangaTitle.Substring(0, mangaTitle.IndexOf("<"));
            
            html = html.Substring(html.IndexOf("<div id=\"chapterlist\">"));

            //<a href="/tobaku-datenroku-kaiji-kazuyahen/1"> Tobaku Datenroku Kaiji: Kazuyahen 1</a> : </td>
            //<td>01/09/2013</td>
            foreach (string item in html.Split(new string[] { "<div class=\"chico_manga\"></div>" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item.IndexOf("chapterlist") == -1)
                {

                    Chapter chap = new Chapter();

                    chap.Url = ConfigurationManager.AppSettings["rootPage"].ToString() + item.Substring(item.IndexOf("\"") + 1, (item.IndexOf(">") - 1 - item.IndexOf("\"")) - 1);
                    chap.Title = item.Substring(item.IndexOf(">") + 1, item.IndexOf("</a>") -2 - item.IndexOf(">") + 1);
                    chap.ExtendTitle = item.Substring(item.IndexOf("</a>") + 4, item.IndexOf("</td>") - item.IndexOf("</a>") - 4).Replace(" : ","");
                    chap.Release = item.Substring(item.IndexOf("<td>") + 4, 10);
                    chap.MangaTitle = mangaTitle;
                    mangas[index].Chapters.Add(chap);
                }
            }

            tvMangas.BeginInvoke((MethodInvoker)delegate
            {
                tvMangas.Nodes[index].Nodes.Clear();
                tvMangas.Nodes[index].Nodes.AddRange(Chapter.generateTree(mangas[index].Chapters));    
            });

            helper.Mre.Set();
        }

        #endregion

        #region Helper Class

        public class ManualResetEventHelper
        {
            private ManualResetEvent mre;
            private object objectoAuxiliar;

            public ManualResetEventHelper(ManualResetEvent pMre, object pObject)
            {
                this.mre = pMre;
                this.objectoAuxiliar = pObject;
            }

            public ManualResetEvent Mre
            {
                get { return mre; }
                set { mre = value; }
            }

            public object ObjetoAuxiliar
            {
                get { return objectoAuxiliar; }
                set { objectoAuxiliar = value; }
            }
        }

        #endregion

        
    }

}
