using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;
using System.Windows.Markup;
using System.Xml;
using System.Threading;

namespace AnnotationProject {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public MainWindow() {
            db = DataUtil.GetDataContext();
            ///Todo: 
            ///Add a library of fundamental texts
            ///Filter annotations based on where you are in the text
            ///Introduce inter-text linking and the notion of chapter, paragraph, etc.
            this.Selection = new TextPosition();
            InitializeComponent();
            this.ShowAll = true;
            this.currentUser = db.Users.First();

            string source = @"C:\Users\Amichai\Dropbox\Share Folder\Literature\books\victor hugo\les miserable.txt";
            string text = string.Concat(System.IO.File.ReadAllText(source).Take(100000));
            
            this.currentTextDetail = db.TextDetails.Where(i => i.Title == "Les Miserables").Single();
            this.currentText = db.Texts.Where(i => i.ID == 1).Single();

            setText(text);
            this.Text.Title = this.currentTextDetail.Title;
            //clearAllTexts();
            if (!db.Texts.Select(i => i.TextDetail.Title).Contains(currentTextDetail.Title)) {
                db.Texts.AddObject(this.currentText);
                db.SaveChanges();
            }
            loadAnnotations();
        }

        private void setText(string text) {
            FlowDocument d = new FlowDocument();
            d.Blocks.Add(new Paragraph(new Run(text)));
            this.body.Document = d;
        }

        TextDetail currentTextDetail;

        private void loadAnnotations() {
            if (this.AvailableAnnotations.Items.Count == db.Annotations.Count()) { return; }
            this.AvailableAnnotations.ItemsSource = null;
            this.AvailableAnnotations.ItemsSource = db.Annotations.ToList();
        }

        private bool same(List<Annotation> a, List<Annotation> b) {
            if (a.Count != b.Count) {
                return false;
            }
            for (int i = 0; i < b.Count; i++) {
                if (a[i] != b[i]) {
                    return false;
                }
            }
            return true;
        }

        private void loadAnnotations(List<Annotation> annotations) {
            ///Todo: this test is incorrect. We need to check that the lists are actually the same
            if (same(annotations, this.AvailableAnnotations.ItemsSource.Cast<Annotation>().ToList())) {
                return;
            }
            this.AvailableAnnotations.ItemsSource = null;
            this.AvailableAnnotations.ItemsSource = annotations;
        }

        table1Entities db;

        private void clearAllTexts() {
            foreach (var t in db.Texts) {
                db.Texts.DeleteObject(t);
            }
        }

        User currentUser;
        Text currentText;

        private void saveAnnotation(object state) {
            var annotationAndTags = (annotationAndTags)state;

            Annotation annotation = new Annotation() {
                StartIndex = this.Selection.CharIndex,
                SourceLength = this.Selection.CharLength,
                SourceText = currentText.ID,
                Content = annotationAndTags.Annotation,
                Author = currentUser.ID,
                UpVotes  =0,
                 DownVotes =0
            };
            db.Annotations.AddObject(annotation);
            db.SaveChanges();

            List<int> tagIDs = new List<int>();
            List<string> inputTags = annotationAndTags.Tags;
            foreach (var tag in inputTags) {
                var resolvedTag = db.Tags.Where(i => i.Name == tag).SingleOrDefault();
                if (resolvedTag == null) {
                    var newTag = new Tag() { Name = tag };
                    db.Tags.AddObject(newTag);
                    db.SaveChanges();
                    tagIDs.Add(newTag.ID);
                } else {
                    tagIDs.Add(resolvedTag.ID);
                }
            }

            foreach (var id in tagIDs) {
                var annotationTag = new AnnotationTag() { AnnotationID = annotation.ID, TagID = id };
                db.AnnotationTags.AddObject(annotationTag);
                db.SaveChanges();
            }

            Dispatcher.Invoke((Action)(() => {
                loadAnnotations();
            }));
        }

        private class annotationAndTags {
            public string Annotation { get; set; }
            public List<string> Tags { get; set; }
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            if (this.Selection.CharLength == 0) {
                ///TODO: notify the user why this failed
                return;
            }

            var flowDocumentText = XamlWriter.Save(this.inputText.Document);
            var tags = this.tags.Text.Split(' ', ',').ToList();
            this.tags.Text = "";
            this.inputText.Document = new FlowDocument();
            var annotationAndTags = new annotationAndTags() { Annotation = flowDocumentText, Tags = tags };
            ThreadPool.QueueUserWorkItem(saveAnnotation, annotationAndTags);
        }

        private TextPosition _selection;

        public TextPosition Selection {
            get { return _selection; }
            set {
                _selection = value;
                OnPropertyChanged("Selection");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private void body_SelectionChanged(object sender, RoutedEventArgs e) {
            var tb = (sender as RichTextBox);
            this.Selection.CharIndex = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.Start);
            this.Selection.CharLength = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.End) - this.Selection.CharIndex;
            int linenum;
            tb.Document.ContentStart.GetLineStartPosition(0, out linenum);
            this.Selection.LineNumber = linenum;

            if (this.ShowAll) return;
            var annotations = db.Annotations.Where(i => (i.StartIndex >= this.Selection.CharIndex && i.StartIndex <= this.Selection.EndIndex) ||
                (i.StartIndex + i.SourceLength >= this.Selection.CharIndex && i.StartIndex + i.SourceLength <= this.Selection.EndIndex) ||
                (i.StartIndex <= this.Selection.CharIndex && i.StartIndex + i.SourceLength >= this.Selection.EndIndex)).ToList() ;
            if (annotations.Any()) {
                loadAnnotations(annotations);
            } else {
                loadAnnotations();
            }
        }

        private void body_LostFocus(object sender, RoutedEventArgs e) {
            // This persists the selection
            e.Handled = true;
        }

        public bool ShowAll {
            get { return _ShowAll; }
            set {
                if (_ShowAll != value) {
                    _ShowAll = value;
                    if(value){
                        loadAnnotations();
                    }
                    OnPropertyChanged(ShowAllPropertyName);
                }
            }
        }
        private bool _ShowAll;
        public const string ShowAllPropertyName = "ShowAll";

        private void AvailableAnnotations_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedAnnotation = ((sender as ListBox).SelectedItem as Annotation);
            if (selectedAnnotation == null) { return; }
            var startIdx = selectedAnnotation.StartIndex.Value;
            var length = selectedAnnotation.SourceLength.Value;

            ///TODO: this is where we set the local selection object
            this.body.Focus();

            this.Selection.CharIndex = startIdx;
            this.Selection.CharLength = length;
            var start = this.body.Document.ContentStart;

            this.body.Selection.Select(start.GetPositionAtOffset(startIdx, LogicalDirection.Forward), start.GetPositionAtOffset(startIdx + length, LogicalDirection.Forward));
            this.selectedAnnotationRoot.DataContext = selectedAnnotation;
            this.annotationText.Document = selectedAnnotation.Content.LoadFlowDocument();
            
        }

        private void ClearAnnotations_Click(object sender, RoutedEventArgs e) {
            db.Annotations.ToList().ForEach(i => db.DeleteObject(i));
            db.AnnotationTags.ToList().ForEach(i => db.DeleteObject(i));
            db.SaveChanges();
            loadAnnotations();
        }

        private void Filter_Click(object sender, RoutedEventArgs e) {
            this.ShowAll = false;
            var search = this.searchTerms.Text.Split(new char[] { ' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var tags = search.Where(i => i.First() == '[' && i.Last() == ']').ToList();
            List<Annotation> annotations;
            if (tags.Count() > 0) {
                foreach (var t in tags) {
                    search.Remove(t);
                }
                tags = tags.Select(i => i.Substring(1, i.Count() - 2)).ToList();

                var ids = db.Tags.Where(t => tags.Contains(t.Name)).Select(i => i.ID);

                var matchedTags = db.AnnotationTags.Where(i => ids.Any(j => i.TagID == j)).ToList();

                ///Filter annotations on content that contains all the search terms
                annotations = matchedTags.Select(i => i.Annotation).Distinct().Where(i =>
                    search.All(j => i.Content.Contains(j))
                    ).ToList();
            } else {

                annotations = db.Annotations.Where(i =>
                    search.All(j => i.Content.Contains(j))
                    ).ToList();
            }
            loadAnnotations(annotations);
        }
    }
}
