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

namespace AnnotationProject.Controls {
    /// <summary>
    /// Interaction logic for TextControl.xaml
    /// </summary>
    public partial class TextControl : UserControl, INotifyPropertyChanged {
        public TextControl(string text, string name) {
            FlowDocument d = new FlowDocument();
            d.Blocks.Add(new Paragraph(new Run(text)));
            InitializeComponent();
            this.body.Document = d;
            this.Selection = new TextPosition();
            this.Title = name;
        }

        public string Title { get; set; }

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

        public event EventHandler lostFocus;

        private void onLostFocus() {
            var eh = lostFocus;
            if (eh != null) {
                eh(this, new EventArgs());
            }
        }

        private void onSelectionChanged() {
            var eh = selectionChanged;
            if (eh != null) {
                eh(this, new EventArgs());
            }
        }

        public event EventHandler selectionChanged;

        private void body_SelectionChanged(object sender, RoutedEventArgs e) {
            var tb = (sender as RichTextBox);
            this.Selection.CharIndex = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.Start);
            this.Selection.CharLength = tb.Document.ContentStart.GetOffsetToPosition(tb.Selection.End) - this.Selection.CharIndex;
            int linenum;
            tb.Document.ContentStart.GetLineStartPosition(0, out linenum);
            this.Selection.LineNumber = linenum;
            onSelectionChanged();
        }

        private void body_LostFocus(object sender, RoutedEventArgs e) {
            // This persists the selection
            e.Handled = true;
            onLostFocus();
        }
    }
}
