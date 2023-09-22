using System;
using System.Data;
using System.Data.Odbc;

namespace socket_c_sharp
{
    public class DataBaseConnection : IDisposable
    {
        private OdbcConnection connection;

        public DataBaseConnection()
        {

            string connectionString = $"DSN=M1";
            connection = new OdbcConnection(connectionString);
        }

        public void Open()
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        public void Close()
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        public void Dispose()
        {
            Close();
            connection.Dispose();
        }

        public DataTable ExecuteQuery(string query)
        {
            using (OdbcCommand cmd = new OdbcCommand(query, connection))
            {
                DataTable dataTable = new DataTable();
                try
                {
                    Open();
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }
                catch (System.Exception)
                {

                    throw;
                }
                finally
                {
                    Close();
                }
                return dataTable;
            }
        }
    }
}