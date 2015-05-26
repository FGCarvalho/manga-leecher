using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaLeecher
{
    public class Chapter
    {
        private string release;
        private string extendTitle;
        private string title;
        private string url;
        private string mangaTitle;
        private List<string> pages;

        public string ExtendTitle
        {
            get { return extendTitle; }
            set { extendTitle = value; }
        }

        public string MangaTitle
        {
            get { return mangaTitle; }
            set { mangaTitle = value; }
        }

        public string Release
        {
            get { return release; }
            set { release = value; }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public List<string> Pages
        {
            get { return pages; }
            set { pages = value; }
        }

        public static TreeNode[] generateTree(List<Chapter> lst)
        {
            List<TreeNode> tn = new List<TreeNode>();

            foreach (Chapter c in lst)
            {
                TreeNode n = new TreeNode();

                n.Text = String.IsNullOrEmpty(c.ExtendTitle) ? c.Title : c.ExtendTitle;

                tn.Add(n);
            }

            return tn.ToArray();
        }
    }
}
