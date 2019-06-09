using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace App.Plugins
{
    public class DBConnection
    {
        public static DataSet GetQuery(string query)
        {
            DataSet dataTable = new DataSet();
            try
            {
                string connString = "Data Source=.;Initial Catalog=ReportServer;User ID=ReportUser;Password=admin";

                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                // create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // this will query your database and return the result to your datatable
                da.Fill(dataTable);
                conn.Close();
                da.Dispose();
                return dataTable;
            } catch (Exception ex)
            {
                return null;
            }
        }
    }
}