using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnnotationProject {
    public class DataUtil {
        public static table1Entities GetDataContext() {
            //return new DataEntities(@"metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=System.Data.SqlServerCe.3.5;provider connection string="";Data Source=c:\users\amichai\documents\visual studio 2010\Projects\AnnotationProject\AnnotationProject\Data.sdf""");
            return new table1Entities(@"metadata=res://*/RemoteDB.csdl|res://*/RemoteDB.ssdl|res://*/RemoteDB.msl;provider=MySql.Data.MySqlClient;provider connection string=""server=198.61.151.117;User Id=amichai;password=password1;database=table1;Persist Security Info=True""");
            
        }
    }
}
