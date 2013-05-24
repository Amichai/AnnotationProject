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
            this.DataContext = this;
            this.Selection = new TextPosition();
            this.annotations = new List<Annotation>();
            InitializeComponent();
            this.currentUser = new User() { Username = "Amichai" };
            this.currentText = new Text() { User = currentUser.Username, Title = "Les Miserables", Source = @"C:\Users\Amichai\Dropbox\Share Folder\Literature\books\victor hugo\les miserable.txt", Content = null, ID = Guid.NewGuid() };
            this.body.Text = string.Concat(System.IO.File.ReadAllText(this.currentText.Source).Take(100000));
            this.Text.Title = this.currentText.Title;
            db = DataUtil.GetDataContext();
            //clearAllTexts();
            if (!db.Texts.Select(i => i.Title).Contains(currentText.Title)) {
                db.Texts.AddObject(this.currentText);
                db.SaveChanges();
            }
            loadAnnotations();

        }

        private void loadAnnotations() {
            foreach (var a in db.References) {
                this.annotations.Add(new Annotation() {
                    StartIndex = a.SourceStartIndex.Value,
                    Length = a.SourceLength.Value,
                    Content = db.Texts.Single(i => i.ParentRef == a.ID).Content
                });
            }
            this.AvailableAnnotations.ItemsSource = annotations;
        }

        DataEntities db;

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
            var db = DataUtil.GetDataContext();
            Reference reference = new Reference() {
                SourceStartIndex = this.Selection.CharIndex,
                SourceLength = this.Selection.CharLength,
                SourceText = currentText.ID,
                ID = Guid.NewGuid(),
            };
            Text newText = new Text() { User = this.currentUser.Username, Content = this.inputText.Text, ParentRef = reference.ID, ID = Guid.NewGuid() };
            db.Texts.AddObject(newText);
            db.SaveChanges();
            db.References.AddObject(reference);
            db.SaveChanges();
            this.inputText.Text = "";
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
    }
}
    