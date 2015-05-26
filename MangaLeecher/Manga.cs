using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Configuration;

namespace MangaLeecher
{
    public class Manga
    {
        public Manga() 
        {
            this.chapters = new List<Chapter>();
        }

        //<li><a href="/junjou-drop"> Junjou Drop</a><span class="mangacompleted">[Completed]</span></li>
        public Manga(string li) 
        {
            this.completed = li.IndexOf("[Completed]") > -1;

            this.url = ConfigurationManager.AppSettings["rootPage"] + li.Substring(li.IndexOf('"') + 1, li.IndexOf("\">") - (1 + li.IndexOf('"')));

            this.name = li.Substring(li.IndexOf("\">") + 2, li.IndexOf("</a>") - (2 + li.IndexOf("\">"))).Trim();

            this.chapters = new List<Chapter>();
        }

        private string name;
        private string url;
        private bool completed;
        private List<Chapter> chapters;

        public List<Chapter> Chapters
        {
            get { return chapters; }
            set { chapters = value; }
        }

        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public static List<Manga> generateList(MatchCollection col){

            List<Manga> lst = new List<Manga>();

            foreach (Match m in col)
            {
                lst.Add(new Manga(m.Value));
            }

            return lst;
        }

        public static List<TreeNode> generateTree(List<Manga> lst)
        {
            List<TreeNode> tn = new List<TreeNode>();

            foreach (Manga m in lst)
            {
                TreeNode n = new TreeNode();

                if (m.completed)
                    n.ForeColor = System.Drawing.Color.DarkBlue;

                n.Text = m.name;

                tn.Add(n);
            }

            return tn;
        }
        
    }
}
