using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Configuration;

namespace MangaLeecher
{
    public class Utils
    {
        public static string requestFullStringPage(string url, int turn = 1)
        {
            string msg = string.Empty;

            try
            {
                WebRequest wr = WebRequest.Create(url);

                if (wr != null)
                {
                    WebResponse wb = wr.GetResponse();

                    if (wb != null)
                    {
                        StreamReader s = new StreamReader(wb.GetResponseStream());

                        if (s != null)
                            msg = s.ReadToEnd();
                    }
                }
            }
            catch (Exception exp)
            {
                if (turn > 50)
                {
                    TextWriter twWaitingToBeFetched;
                    twWaitingToBeFetched = TextWriter.Synchronized(new StreamWriter(ConfigurationManager.AppSettings["rootDirectory"] + "logErro.txt", true));
                    twWaitingToBeFetched.WriteLine(DateTime.Now.ToString("yyyy-MM-dd") + "[ERROR] " + url);
                    twWaitingToBeFetched.WriteLine(DateTime.Now.ToString("yyyy-MM-dd") + "[ERROR] " + exp.Message);
                    twWaitingToBeFetched.Flush();
                    twWaitingToBeFetched.Dispose();
                    twWaitingToBeFetched = null;
                }
                else
                {
                    //Aguarda para testar novamente
                    System.Threading.Thread.Sleep(1500);
                }

                return requestFullStringPage(url, ++turn);
            }


            return msg;
        }

        public static Stream requestStream(string url)
        {
            Stream stream = null;

            WebRequest wr = WebRequest.Create(url);

            if (wr != null)
            {
                WebResponse wb = wr.GetResponse();

                if (wb != null)
                {
                    stream = wb.GetResponseStream();
                }
            }

            return stream;
        }
    }
}
