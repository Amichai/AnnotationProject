using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnnotationProject {
    class DataUtil {

        public static DataEntities GetDataContext() {
            return new DataEntities(@"metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=System.Data.SqlServerCe.3.5;provider connection string="";Data Source=c:\users\amichai\documents\visual studio 2010\Projects\AnnotationProject\AnnotationProject\Data.sdf""");
        }
    }
}
