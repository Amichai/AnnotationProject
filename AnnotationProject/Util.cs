using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.IO;
using System.Xml;
using System.Windows.Markup;

namespace AnnotationProject {
    public static class Util {
        public static FlowDocument LoadFlowDocument(this string content) {
            var fd = new FlowDocument();
            if (string.Concat(content.Take(13)) == "<FlowDocument") {
                var stringReader = new StringReader(content);
                var xmlTextReader = new XmlTextReader(stringReader);
                fd = (FlowDocument)XamlReader.Load(xmlTextReader);
            } else {
                Paragraph p = new Paragraph(new Run(content));
                fd.Blocks.Add(p);
            }
            return fd;
        }

        public static string GetFlowDocumentText(this string content) {
            var fd = content.LoadFlowDocument();
            return new TextRange(fd.ContentStart, fd.ContentEnd).Text;
        }
    }
}
