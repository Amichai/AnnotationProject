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

namespace AnnotationProject {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        List<Annotation> annotations;
        public MainWindow() {
            db = DataUtil.GetDataContext();
            ///Todo: 
            ///Add a library of fundamental texts
            ///Filter annotations based on where you are in the text
            ///Introduce inter-text linking and the notion of chapter, paragraph, etc.
            this.DataContext = this;
            this.Selection = new TextPosition();
            this.annotations = new List<Annotation>();
            InitializeComponent();
            
            this.currentUser = new User() { Name = "Amichai" };
            
            string source = @"C:\Users\Amichai\Dropbox\Share Folder\Literature\books\victor hugo\les miserable.txt";
            string text = string.Concat(System.IO.File.ReadAllText(source).Take(100000));
            this.currentTextDetail = db.TextDetails.Where(i => i.Title == "Les Miserables").Single();
            this.currentText = db.Texts.Where(i => i.ID == 1).Single();

            this.body.Text = text;
            this.Text.Title = this.currentTextDetail.Title;
            //clearAllTexts();
            if (!db.Texts.Select(i => i.TextDetail.Title).Contains(currentTextDetail.Title)) {
                db.Texts.AddObject(this.currentText);
                db.SaveChanges();
            }
            loadAnnotations();
        }

        TextDetail currentTextDetail;

        private void loadAnnotations() {
            this.annotations.Clear();
            foreach (var a in db.Annotations) {
                this.annotations.Add(new Annotation() {
                    StartIndex = a.StartIndex.Value,
                    SourceLength = a.SourceLength.Value,
                    Content = a.Content,
                    UpVotes = 12
                });
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

        private void Save_Click(object sender, RoutedEventArgs e) {
            if (this.Selection.CharLength == 0) {
                return;
            }
            
            Annotation annotation = new Annotation() {
                StartIndex = this.Selection.CharIndex,
                SourceLength = this.Selection.CharLength,
                SourceText = currentText.ID,
                Content = inputText.Text

            };
            db.Annotations.AddObject(annotation);
            db.SaveChanges();
            this.inputText.Text = "";
            loadAnnotations();
            ///Save a annotation to a DB
            ///Add anoter panel for visualizing all annotations
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
            var tb = (sender as TextBox);
            this.Selection.CharIndex = tb.SelectionStart;
            this.Selection.CharLength = tb.SelectionLength;
        }

        private void body_LostFocus(object sender, RoutedEventArgs e) {
            // This persists the selection
            e.Handled = true;
        }         

        private void AvailableAnnotations_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            var annotate = ((sender as ListBox).SelectedItem as Annotation);
            var startIdx = annotate.StartIndex;
            var length = annotate.SourceLength;
            this.body.Select(startIdx.Value, length.Value);
        }
    }
}
    