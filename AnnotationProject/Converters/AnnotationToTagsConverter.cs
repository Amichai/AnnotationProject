using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AnnotationProject.Converters {
    class AnnotationToTagsConverter : IValueConverter{
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var annotation = value as Annotation;
            using (var db = DataUtil.GetDataContext()) {
                var text = string.Concat(db.AnnotationTags.Where(i => i.AnnotationID == annotation.ID).Select(i => i.Tag.Name + " "));
                return text;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
