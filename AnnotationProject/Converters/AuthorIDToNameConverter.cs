using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AnnotationProject.Converters {
    class AuthorIDToNameConverter : IValueConverter{
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null) { return ""; }
            int id = (int)value;
            using (var db = DataUtil.GetDataContext()) {
                return db.Users.Where(i => i.ID == id).Single().Name;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
