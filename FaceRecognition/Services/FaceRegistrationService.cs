using FaceRecognition.Data;
using FaceRecognition.Helpers;
using System.Text.Json;

namespace FaceRecognition.Services
{
    public class FaceRegistrationService
    {
        private readonly MySqlConnectionFactory _db;

        private readonly FaceModelService _model;

        public FaceRegistrationService(
            MySqlConnectionFactory db,
            FaceModelService model)
        {
            _db = db;
            _model = model;
        }

        public async Task RegisterAsync(
            long personId, string employeeName,
            IFormFile image)
        {
            if (personId <= 0)
            {
                throw new Exception(
                    "Valid PersonId is required.");
            }

            if (string.IsNullOrWhiteSpace(employeeName))
            {
                throw new Exception(
                    "Employee name is required.");
            }

            if (image == null ||
                image.Length == 0)
            {
                throw new Exception(
                    "Image is required.");
            }

            // Image -> embedding
            var embedding =
                await _model.GenerateEmbeddingAsync(
                    image);

            // Embedding -> JSON
            var embeddingJson =
                JsonSerializer.Serialize(embedding);

            await using var connection =
                _db.CreateConnection();

            await connection.OpenAsync();

            const string sql = """
            INSERT INTO face_embedding
            (
                PersonId,
                EmployeeName,
                Embedding,
                CreatedAt
            )
            VALUES
            (
                @PersonId,
                @EmployeeName,
                @Embedding,
                NOW()
            )
            ON DUPLICATE KEY UPDATE
                EmployeeName = @EmployeeName,
                Embedding = @Embedding,
                CreatedAt = NOW();
            """;

            await using var command =
                new MySqlConnector.MySqlCommand(
                    sql,
                    connection);

            command.Parameters.AddWithValue(
                "@PersonId",
                personId);

            command.Parameters.AddWithValue(
                "@EmployeeName",
                employeeName.Trim());

            command.Parameters.AddWithValue(
                "@Embedding",
                embeddingJson);

            await command.ExecuteNonQueryAsync();
        }


    }
}
