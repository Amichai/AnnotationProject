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
            var stringReader = new StringReader(content);
            var xmlTextReader = new XmlTextReader(stringReader);
            return (FlowDocument)XamlReader.Load(xmlTextReader);
        }

        public static string GetFlowDocumentText(this string content) {
            var fd = content.LoadFlowDocument();
            return new TextRange(fd.ContentStart, fd.ContentEnd).Text;
        }
    }
}
