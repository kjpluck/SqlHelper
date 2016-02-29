using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace KevsSqlHelper
{
    public static class Sql
    {
        public static string ConnectionString { get; set; }
        public static string LastStoredProcedureCalled { get; private set; }
        
        private static SqlCommand StoredProcedure(SqlConnection connection, string name)
        {
            LastStoredProcedureCalled = name;

            return new SqlCommand
            {
                CommandText = name,
                CommandType = CommandType.StoredProcedure,
                Connection = connection
            };
        }

        public static SqlParameterCollection CallStoredProcedure(string name, SqlParameter parameter)
        {
            return CallStoredProcedure(name, new List<SqlParameter> {parameter});
        }

        public static SqlParameterCollection CallStoredProcedure(string name, List<SqlParameter> parameters)
        {

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();

                var storedProcedure = MakeStoredProcedure(sqlConnection, name, parameters);
                storedProcedure.ExecuteNonQuery();

                return storedProcedure.Parameters;
            }

        }

        public static List<T> CallSpReturningListOf<T>(string name, SqlParameter parameter) where T : new()
        {
            var spParameterList = new List<SqlParameter> { parameter };
            return CallSpReturningListOf<T>(name, spParameterList);
        }

        public static List<T> CallSpReturningListOf<T>(string name, List<SqlParameter> parameters) where T : new()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                var storedProcedure = MakeStoredProcedure(sqlConnection, name, parameters);

                using (var reader = storedProcedure.ExecuteReader())
                {
                    return GetItems<T>(reader);
                }
            }
        }

        private static List<T> GetItems<T>(SqlDataReader reader) where T : new()
        {
            var items = new List<T>();
            var itemType = typeof(T);
            var itemProperties = new Dictionary<string, PropertyInfo>();

            foreach (var itemProperty in itemType.GetProperties())
            {
                itemProperties[itemProperty.Name.ToUpper()] = itemProperty;
            }

            while (reader.Read())
            {
                items.Add(CreateItem<T>(reader, itemProperties));
            }

            return items;
        }

        private static T CreateItem<T>(SqlDataReader reader, Dictionary<string, PropertyInfo> itemProperties) where T : new()
        {
            var newItem = new T();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (!itemProperties.ContainsKey(reader.GetName(i).ToUpper()))
                    continue;

                var itemProperty = itemProperties[reader.GetName(i).ToUpper()];
                if (!itemProperty.CanWrite)
                    continue;

                try
                {
                    itemProperty.SetValue(newItem, reader.GetValue(i), null);
                }
                catch (Exception e)
                {
                    throw new Exception($"Not able to set \"{itemProperty.Name}\" to \"{reader.GetValue(i)}\"",e);
                }
            }
            return newItem;
        }

        private static SqlCommand MakeStoredProcedure(SqlConnection connection, string name, List<SqlParameter> parameters)
        {
            var storedProcedure = StoredProcedure(connection, name);

            foreach (var parameter in parameters)
            {
                storedProcedure.Parameters.Add(parameter);
            }
            return storedProcedure;
        }

        public static SqlParameter Param(string name, object value = null, SqlDbType sqlDbType = SqlDbType.NVarChar,
            ParameterDirection direction = ParameterDirection.Input)
        {
            return new SqlParameter
            {
                ParameterName = name,
                SqlDbType = sqlDbType,
                Direction = direction,
                Value = value,
                Size = -1
            };
        }

        public static SqlParameter OutParam(string name, object value = null, SqlDbType sqlDbType = SqlDbType.NVarChar)
        {
            return Param(name, value, sqlDbType, ParameterDirection.Output);
        }

        public static SqlParameter Param(string name, long value)
        {
            return Param(name, value, SqlDbType.BigInt);
        }

        [Obsolete("SingleParam is deprecated, functions now have overloads that accept single parameters.")]
        public static List<SqlParameter> SingleParam(string name, object value = null,
            SqlDbType sqlDbType = SqlDbType.NVarChar,
            ParameterDirection direction = ParameterDirection.Input)
        {
            return new List<SqlParameter> { Param(name, value, sqlDbType, direction) };
        }

        [Obsolete("SingleParam is deprecated, functions now have overloads that accept single parameters.")]
        public static List<SqlParameter> SingleParam(string name, long value)
        {
            return new List<SqlParameter> { Param(name, value) };
        }

        public static T CallSpReturning<T>(string name, SqlParameter parameter) where T : new()
        {
            return CallSpReturning<T>(name, new List<SqlParameter> {parameter});
        }

        public static T CallSpReturning<T>(string name, List<SqlParameter> parameters) where T : new()
        {
            var result = CallSpReturningListOf<T>(name, parameters);
            return result.FirstOrDefault();
        }

        public static void CallSql(string sql)
        {

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                var cmd = new SqlCommand
                {
                    CommandText = sql,
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.ExecuteNonQuery();

            }
        }
    }
}
