using System;
using HBus.Server.Data;
using MySql.Data.MySqlClient;

namespace HBus.Server.Processors
{
    public class MySqlProcessor : BaseProcessor
    {
        private const string Channel = "mysql";

        MySqlConnection _conn;
        private readonly string _cs;

        public MySqlProcessor(string connectionString)
        {
            try
            {

                _conn = new MySqlConnection(connectionString);

                //Event from ep source
                OnSourceEvent = WriteEvent;

                //Error from ep source
                OnSourceError = WriteError;

                //Close connection with ep source
                OnSourceClose = (sender) =>
                {
                    _conn.Close();

                    Log.Debug("MySql connection closed on source close");
                };

                _conn.Open();
                Log.Debug("MySql endpoint created");
            }
            catch (Exception ex)
            {
                Log.Error("MySqlEp init failed", ex);
            }
        }

        private void WriteEvent(Event @event, BaseProcessor sender)
        {
            try
            {
                //_conn = new MySqlConnection(_cs);
                //_conn.Open();

                const string stm = @"INSERT INTO events (name, source, type, value, status, unit, timestamp)
                                     VALUES(@Name, @Source, @Type, @Value, @Status, @Unit, @Timestamp)";

                var cmd = new MySqlCommand(stm, _conn);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Name", @event.Name);
                cmd.Parameters.AddWithValue("@Source", @event.Source);
                cmd.Parameters.AddWithValue("@Type", @event.Channel);
                cmd.Parameters.AddWithValue("@Value", @event.Value);
                cmd.Parameters.AddWithValue("@Status", @event.Status);
                cmd.Parameters.AddWithValue("@Unit", @event.Unit);
                cmd.Parameters.AddWithValue("@Timestamp", @event.Timestamp);
                //Altri parametri
                cmd.ExecuteNonQuery();

                Log.Debug("Mysql written event");

            }
            catch (MySqlException ex)
            {
                //TODO LOG
                Log.Error("Mysql write event error", ex);
            }
            //finally
            //{
            //    //_conn?.Close();
            //    if (_conn != null)
            //        _conn.Close();
            //}
        }
        private void WriteError(Exception error, BaseProcessor sender)
        {
            try
            {
                //_conn = new MySqlConnection(_cs);
                //_conn.Open();

                const string stm = @"INSERT INTO hbus_errors (error, source, inner, timestamp)
                                     VALUES(@Error, @Source, @Inner, @Timestamp)";

                var cmd = new MySqlCommand(stm, _conn);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@Error", error.Message);
                cmd.Parameters.AddWithValue("@Source", error.Source);
                cmd.Parameters.AddWithValue("@Inner", error.InnerException != null ? error.InnerException.Message : string.Empty);
                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                //Altri parametri
                cmd.ExecuteNonQuery();

                Log.Debug("Mysql written error");

            }
            catch (MySqlException ex)
            {
                //TODO LOG
                Log.Error("Mysql write error error", ex);
            }
            //finally
            //{
            //    //_conn?.Close();
            //    if (_conn != null)
            //        _conn.Close();
            //}
        }
    }
}