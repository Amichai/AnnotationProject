using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Documents;

namespace AnnotationProject {
    public class TextPosition : INotifyPropertyChanged{
        private int _charIndex;
        public int CharIndex {
            get {
                return _charIndex;
            }
            set {
                _charIndex = value;
                OnPropertyChanged("CharIndex");
            }
        }
        private int _CharLength;

        private int _lineNumber;

        public int LineNumber {
            get { return _lineNumber; }
            set { 
                _lineNumber = value;
                OnPropertyChanged("LineNumber");
            }
        }
        

        public int CharLength {
            get {
                return _CharLength;
            }
            set {
                _CharLength = value;
                OnPropertyChanged("CharLength");
            }
        }
        
        public int EndIndex {
            get {
                return CharIndex + CharLength;
            }
        }        
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
