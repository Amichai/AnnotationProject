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
using Microsoft.Win32;
using AvalonDock.Layout;
using AnnotationProject.Controls;

namespace AnnotationProject {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public MainWindow() {
            /// TODO:
            /// Every annotation needs a timestam
            /// Also, we will add an edits table for handling diffs and revisions
            /// Edits are also timestamped
            /// By comparing timestamps we can preserve annotation indexing

           ///Show the number of annotations on the text selection page
           ///Highlight the 


            db = DataUtil.GetDataContext();
            ///Todo: 
            ///Add a library of fundamental texts
            ///Filter annotations based on where you are in the text
            ///Introduce inter-text linking and the notion of chapter, paragraph, etc.
            InitializeComponent();
            this.ShowAll = true;
            this.currentUser = db.Users.First();

            this.currentTextDetail = db.TextDetails.Where(i => i.Title == "Les Miserables").Single();
            this.currentText = db.Texts.Where(i => i.ID == 1).Single();

            if (!db.Texts.Select(i => i.TextDetail.Title).Contains(currentTextDetail.Title)) {
                db.Texts.AddObject(this.currentText);
                db.SaveChanges();
            }
            loadAnnotations();

            this.allTexts.ItemsSource = db.Texts.Select(i => i.TextDetail);
        }

        TextDetail currentTextDetail;

        private void loadAnnotations() {
            if (this.AvailableAnnotations.Items.Count == db.Annotations.Count()) { return; }
            this.AvailableAnnotations.ItemsSource = null;
            //List<int> sourceTextIds = db.Texts.Where(i => this.openTexts.Contains(i.TextDetail.Title)).Select(i=> i.ID).ToList();
            //this.AvailableAnnotations.ItemsSource = db.Annotations.Where(i => sourceTextIds.Contains(i.SourceText.Value));
            var text = db.Texts.FirstOrDefault(i => i.TextDetail.Title == this.textRoot.SelectedContent.Title);
            if (text == null) { return; }
            var textId = text.ID;
            this.AvailableAnnotations.ItemsSource = db.Annotations.Where(i => i.SourceText == textId);
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
                StartIndex = currentSelection.CharIndex,
                SourceLength = currentSelection.CharLength,
                SourceText = currentText.ID,
                Content = annotationAndTags.Annotation,
                Author = currentUser.ID,
                UpVotes = 0,
                DownVotes = 0,
                HighlightedSourceText = string.Concat((this.textRoot.SelectedContent.Content as TextControl).body.Selection.Text.Take(100)),
                Timestamp = DateTime.Now
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



        private TextPosition currentSelection {
            get {
                return (this.textRoot.SelectedContent.Content as TextControl).Selection;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            if (currentSelection.CharLength == 0) {
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private void body_SelectionChanged(object sender, RoutedEventArgs e) {
            var tb = (sender as RichTextBox);
            currentSelection.CharIndex = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.Start);
            currentSelection.CharLength = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.End) - currentSelection.CharIndex;
            int linenum;
            tb.Document.ContentStart.GetLineStartPosition(0, out linenum);
            currentSelection.LineNumber = linenum;

            if (this.ShowAll) return;
            var annotations = db.Annotations.Where(i => (i.StartIndex >= currentSelection.CharIndex && i.StartIndex <= currentSelection.EndIndex) ||
                (i.StartIndex + i.SourceLength >= currentSelection.CharIndex && i.StartIndex + i.SourceLength <= currentSelection.EndIndex) ||
                (i.StartIndex <= currentSelection.CharIndex && i.StartIndex + i.SourceLength >= currentSelection.EndIndex)).ToList() ;
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

        private Annotation selectedAnnotation() {
            var annotation = (this.AvailableAnnotations.SelectedItem as Annotation);
            if (annotation == null) {
                return null;
            }
            var id = annotation.ID;
            return db.Annotations.Where(i => i.ID == id).Single();
        }

        private void AvailableAnnotations_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var currentAnnotation = selectedAnnotation();
            if (currentAnnotation == null) { return; }
            List<TextControl> allTextControls = new List<TextControl>();
            foreach (var a in this.textRoot.Children) {
                var asTextControl = a.Content as TextControl;
                if (asTextControl == null) continue;
                allTextControls.Add(asTextControl);
            }
            string inspectionTitle = db.Texts.First(i => i.ID == currentAnnotation.SourceText.Value).TextDetail.Title;
            var selectedTextControl = allTextControls.First(i => i.Title == inspectionTitle);
            var startIdx = currentAnnotation.StartIndex.Value;
            var length = currentAnnotation.SourceLength.Value;
            if (selectedTextControl == null) return;
            selectedTextControl.body.Focus();
            selectedTextControl.Selection.CharIndex = startIdx;
            selectedTextControl.Selection.CharLength = length;
            var start = selectedTextControl.body.Document.ContentStart;
            int jumpTo;
            int textLength = new TextRange(start, selectedTextControl.body.Document.ContentEnd).Text.Count();
            if (currentAnnotation.AnnotationTags.Where(i => i.Tag.Name == "link").Count() > 0
                && int.TryParse(currentAnnotation.Content.GetFlowDocumentText(), out jumpTo) && jumpTo < textLength) {
                        selectedTextControl.body.Selection.Select(
                            start.GetPositionAtOffset(jumpTo, LogicalDirection.Forward), start.GetPositionAtOffset(jumpTo, LogicalDirection.Forward));
                   
            } else {
                //We didn't click a hyperlink
                selectedTextControl.body.Selection.Select(start.GetPositionAtOffset(startIdx, LogicalDirection.Forward), start.GetPositionAtOffset(startIdx + length, LogicalDirection.Forward));
            }

            this.selectedAnnotationRoot.DataContext = currentAnnotation;
            this.annotationText.Document = currentAnnotation.Content.LoadFlowDocument();

            setArrowHighlights();
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

        private UserVote pastVote() {
            var annotation = selectedAnnotation();
            if (annotation == null) return null;
            var currentAnnotationID = annotation.ID;
            var allVotes = db.UserVotes.Where(i => i.UserID == this.currentUser.ID && i.AnnotationID == currentAnnotationID);
            return allVotes.SingleOrDefault();
        }

        private int currentAnnotationID {
            get {
                return selectedAnnotation().ID;
            }
        }

        private void UpVote_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            var vote = pastVote();
            var annotation = selectedAnnotation();
            if (annotation == null) return;
            if (vote == null) {
                var newVote = new UserVote() {
                    UserID = this.currentUser.ID,
                    AnnotationID = currentAnnotationID,
                    Vote = true
                };
                db.UserVotes.AddObject(newVote);
                annotation.UpVotes++;
                highlightUpArrow();
            } else {
                if (vote.Vote.Value) {
                    annotation.UpVotes--;
                    db.UserVotes.DeleteObject(vote);
                    resetArrows();
                } else {
                    vote.Vote = true;
                    annotation.DownVotes--;
                    annotation.UpVotes++;
                    highlightUpArrow();
                }
            }
            db.SaveChanges();
        }

        private void setArrowHighlights() {
            var vote = pastVote();
            if (vote != null) {
                if (vote.Vote.Value) {
                    highlightUpArrow();
                } else {
                    highlightDownArrow();
                }
            } else {
                resetArrows();
            }
        }

        private void resetArrows() {
            this.downArrow.Fill = Brushes.Black;
            this.upArrow.Fill = Brushes.Black;
        }

        private void highlightDownArrow() {
            this.downArrow.Fill = Brushes.Red;
            this.upArrow.Fill = Brushes.Black;
        }

        private void highlightUpArrow() {
            this.downArrow.Fill = Brushes.Black;
            this.upArrow.Fill = Brushes.Red;
        }

        private void DownVote_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            var pastVte = pastVote();
            var annotation = selectedAnnotation();
            if (annotation == null) return;
            if (pastVte == null) {
                ///New vote
                var newVote = new UserVote() {
                    UserID = this.currentUser.ID,
                    AnnotationID = currentAnnotationID,
                    Vote = false
                };
                annotation.DownVotes++;
                highlightDownArrow();
                db.UserVotes.AddObject(newVote);
            } else {
                if (!pastVte.Vote.Value) {
                    annotation.DownVotes--;
                    db.UserVotes.DeleteObject(pastVte);
                    resetArrows();
                } else {
                    pastVte.Vote = false;
                    annotation.UpVotes--;
                    annotation.DownVotes++;
                    highlightDownArrow();
                }
            }
            db.SaveChanges();
        }

        private void Browse_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "*.txt";
            ofd.Multiselect = false;
            ofd.Filter = "Text|*.txt";
            ofd.ShowDialog();
            this.filepath.Text = ofd.FileName;
        }

        private void Upload_Click(object sender, RoutedEventArgs e) {
            var title = this.fileTitle.Text;
            var author = this.fileAuthor.Text;
            var tags = this.tags.Text;
            var filepath = this.filepath.Text;
            if (title == "" || author == "" || filepath == "") {
                return;
            }
            var textDetail = new TextDetail() {
            Author = author,
            Title = title, 
            TextSource = filepath,
            };
            db.TextDetails.AddObject(textDetail);
            db.SaveChanges();
            var newText = new Text() {
                Details = textDetail.ID,
                Content = string.Concat(System.IO.File.ReadAllText(filepath).Take(100000))
            };
            db.Texts.AddObject(newText);
            db.SaveChanges();
            this.allTexts.ItemsSource = null;
            this.allTexts.ItemsSource = db.Texts.Select(i => i.TextDetail);
        }

        List<string> openTexts = new List<string>();

        private void OpenText_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            StackPanel sp = sender as StackPanel;
            var textDetail = sp.Tag as TextDetail;
            LayoutDocument layoutDoc = new LayoutDocument();
            layoutDoc.FloatingWidth = 500;
            layoutDoc.FloatingLeft = 500;
            layoutDoc.CanFloat = true;
            layoutDoc.CanClose = true;
            layoutDoc.Title = textDetail.Title;
            var textControl = new TextControl(textDetail.Texts.First().Content, textDetail.Title);
            openTexts.Add(textDetail.Title);
            textControl.selectionChanged += new EventHandler(textControl_selectionChanged);
            layoutDoc.Content = textControl;
            layoutDoc.Closing += new EventHandler<CancelEventArgs>(layoutDoc_Closing);
            layoutDoc.IsSelectedChanged += new EventHandler(layoutDoc_IsSelectedChanged);
            this.textRoot.Children.Insert(0, layoutDoc);
            this.textRoot.Children[0].IsSelected = true;
            loadAnnotations();
        }

        void layoutDoc_IsSelectedChanged(object sender, EventArgs e) {
            if((sender as LayoutDocument).IsSelected){
                loadAnnotations();
            }
        }

        void layoutDoc_Closing(object sender, CancelEventArgs e) {
            var title = ((sender as LayoutDocument).Content as TextControl).Title;
            openTexts.Remove(title);
            loadAnnotations();
        }

        void textControl_selectionChanged(object sender, EventArgs e) {
            var textControl = sender as TextControl;
            var selection = textControl.Selection;
            if (this.ShowAll) return;
            var annotations = db.Annotations.Where(i => (i.StartIndex >= selection.CharIndex && i.StartIndex <= selection.EndIndex) ||
                (i.StartIndex + i.SourceLength >= selection.CharIndex && i.StartIndex + i.SourceLength <= selection.EndIndex) ||
                (i.StartIndex <= selection.CharIndex && i.StartIndex + i.SourceLength >= selection.EndIndex)).ToList();
            if (annotations.Any()) {
                loadAnnotations(annotations);
            } else {
                loadAnnotations();
            }
        }
    }
}
