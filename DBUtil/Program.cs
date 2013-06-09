using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnnotationProject;

namespace DBUtil {
    class Program {
        static void Main(string[] args) {
            var db = DataUtil.GetDataContext();
            clearAnnotations(db);
        }

        private static void clearAnnotations(table1Entities db){
            db.Annotations.ToList().ForEach(i => db.DeleteObject(i));
            db.SaveChanges();
        }
    }
}
