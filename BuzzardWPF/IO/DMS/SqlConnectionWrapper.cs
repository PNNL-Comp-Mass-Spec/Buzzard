using System;
using System.Data;
using System.Data.SqlClient;

namespace BuzzardWPF.IO.DMS
{
    /// <summary>
    /// A SqlConnection wrapper that only disposes in certain circumstances
    /// </summary>
    internal class SqlConnectionWrapper : IDisposable
    {
        private readonly SqlConnection connection;
        private readonly bool closeConnectionOnDispose;
        public string FailedConnectionAttemptMessage { get; }

        /// <summary>
        /// Open a new connection, which will get closed on Dispose().
        /// </summary>
        /// <param name="connString"></param>
        public SqlConnectionWrapper(string connString)
        {
            try
            {
                connection = new SqlConnection(connString);
                connection.Open();
            }
            catch (Exception e)
            {
                FailedConnectionAttemptMessage =
                    $"Error connecting to database; Please check network connections and try again. Exception message: {e.Message}";
            }

            closeConnectionOnDispose = true;
            IsValid = connection?.State == ConnectionState.Open;
        }

        /// <summary>
        /// Wrap an existing connection, which will stay open on Dispose().
        /// </summary>
        /// <param name="existingConnection"></param>
        /// <param name="failedConnectionAttemptMessage"></param>
        public SqlConnectionWrapper(SqlConnection existingConnection, string failedConnectionAttemptMessage = "")
        {
            connection = existingConnection;
            closeConnectionOnDispose = false;
            IsValid = connection?.State == ConnectionState.Open;
            FailedConnectionAttemptMessage = failedConnectionAttemptMessage;
        }

        public bool IsValid { get; }

        public SqlConnection GetConnection()
        {
            return connection;
        }

        public SqlCommand CreateCommand()
        {
            return connection.CreateCommand();
        }

        // ReSharper disable once UnusedMember.Local
        public SqlTransaction BeginTransaction()
        {
            return connection.BeginTransaction();
        }

        public void Dispose()
        {
            if (!closeConnectionOnDispose)
            {
                return;
            }

            connection?.Close();
            connection?.Dispose();
        }
    }
}