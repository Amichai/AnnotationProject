using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnnotationProject;
using System.Net;
using System.IO;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DBUtil {
    class Program {
        static table1Entities db = DataUtil.GetDataContext();
        static void Main(string[] args) {
            //clearAnnotations(db);
            //paradiseLost();
            addText();

        }

        private static void addText() {
            string textUrl = @"http://www.dartmouth.edu/~milton/reading_room/pl/book_1/text.shtml";
            var cont = getContent(textUrl);
            var milton = db.Texts.ToList()[3];
            milton.Content = cont;

            db.SaveChanges();

            //TextDetail detail = new TextDetail() {
            //    Author = "John Milton",
            //    Title = "Paradise Lost",
            //    TextSource = textUrl
            //};
            //db.TextDetails.AddObject(detail);
            //db.SaveChanges();

            //var text = new Text() {
            //    Content = cont,
            //    Details = detail.ID,
            //};
            //db.Texts.AddObject(text);
            //db.SaveChanges();
        }



        private static void paradiseLost() {
            string notesUrl = @"http://www.dartmouth.edu/~milton/reading_room/pl/book_1/notes.shtml";
            string textUrl = @"http://www.dartmouth.edu/~milton/reading_room/pl/book_1/text.shtml";
            var cont = getContent(textUrl);
            string regex = "\\[(?:.|\n)*?\\]";
            string output = Regex.Replace(cont, regex, "");

            var links = getAllLinks(notesUrl);

            int lastIndex = 0;
            foreach (var l in getAllLinks(notesUrl)) {
                var index = cont.ToLower().IndexOf(l.Item1.ToLower(), lastIndex);
                if (index == -1) {
                    continue;
                }
                lastIndex = index;

                Debug.Print(index.ToString());

                Annotation annotation = new Annotation() {
                    HighlightedSourceText = l.Item1,
                    SourceLength = l.Item1.Count(),
                    StartIndex = index + 2,
                    Timestamp = DateTime.Now,
                    Content = l.Item2,
                    SourceText = 5,
                };
                db.Annotations.AddObject(annotation);
            }
            db.SaveChanges();
        }


        public static IEnumerable<Tuple<string, string>> getAllLinks(string webAddress) {
            HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
            HtmlDocument newdoc = web.Load(webAddress);
            var selected = newdoc.DocumentNode.SelectNodes("//a").Where(i => i.Attributes["class"] != null && i.Attributes["class"].Value == "note");

            foreach (var s in selected) {
                var sourceText = s.InnerText;
                var comment = string.Concat(s.ParentNode.InnerText);
                var idx = comment.IndexOf('.');
                comment = string.Concat(comment.Skip(idx)).TrimStart(' ', '.').TrimEnd('\n');

                yield return new Tuple<string, string>(sourceText, comment);
            }
        }


        public static string getContent(string webAddress) {
            HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
            HtmlDocument doc = web.Load(webAddress);

            return string.Join(" ", doc.DocumentNode.Descendants().Select(x => x.InnerText));
        }

        public static string GetContent(string url) {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            var response = req.GetResponse();
            var streamResponse = response.GetResponseStream();

            StreamReader streamRead = new StreamReader(streamResponse);
            Char[] readBuffer = new Char[256];
            int count = streamRead.Read(readBuffer, 0, 256);
            string content = "";
            while (count > 0) {
                String outputData = new String(readBuffer, 0, count);
                content += outputData;
                count = streamRead.Read(readBuffer, 0, 256);
            }
            streamRead.Close();
            streamResponse.Close();
            // Release the response object resources.
            streamResponse.Close();
            return content;
        }


        private static void clearAnnotations(table1Entities db){
            db.Annotations.ToList().ForEach(i => db.DeleteObject(i));
            db.SaveChanges();
        }
    }
}
