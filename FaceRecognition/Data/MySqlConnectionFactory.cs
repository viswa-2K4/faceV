


using MySqlConnector;


namespace FaceRecognition.Data
{
    public class MySqlConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public MySqlConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MySqlConnection CreateConnection()
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            return new MySqlConnection(connectionString);
        }
    }
}
