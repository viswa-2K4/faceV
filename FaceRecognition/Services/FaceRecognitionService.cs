using FaceRecognition.Data;
using FaceRecognition.Helpers;
using FaceRecognition.Models;
using System.Text.Json;

namespace FaceRecognition.Services
{
    public class FaceRecognitionService
    {
        private readonly MySqlConnectionFactory _db;

        private readonly FaceModelService _model;

        private readonly IConfiguration _configuration;

        public FaceRecognitionService(
            MySqlConnectionFactory db,
            FaceModelService model,
            IConfiguration configuration)
        {
            _db = db;
            _model = model;
            _configuration = configuration;
        }

        public async Task<RecognitionResponse>
            RecognizeAsync(IFormFile image)
        {
            if (image == null ||
                image.Length == 0)
            {
                return new RecognitionResponse
                {
                    Success = false,
                    Message = "Image is required."
                };
            }

            // Image -> embedding
            var inputEmbedding =
                await _model.GenerateEmbeddingAsync(
                    image);

            await using var connection =
                _db.CreateConnection();

            await connection.OpenAsync();

            const string sql = """
            SELECT PersonId, EmployeeName, Embedding
            FROM face_embedding;
            """;

            await using var command =
                new MySqlConnector.MySqlCommand(
                    sql,
                    connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            long? bestPersonId = null;
            string? bestEmployeeName = null;

            float bestSimilarity = -1;

            while (await reader.ReadAsync())
            {
                var personId =
                    reader.GetInt64("PersonId");

                var employeeName =
      reader.IsDBNull(
          reader.GetOrdinal("EmployeeName"))
          ? null
          : reader.GetString("EmployeeName");


                var json =
                    reader.GetString("Embedding");

                var storedEmbedding =
                    JsonSerializer
                        .Deserialize<float[]>(json);

                if (storedEmbedding == null)
                    continue;

                var similarity =
                    SimilarityHelper.CosineSimilarity(
                        inputEmbedding,
                        storedEmbedding);

                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;

                    bestPersonId = personId;
                    bestEmployeeName = employeeName;
                }
            }

            var threshold =
                _configuration.GetValue<float>(
                    "FaceRecognition:SimilarityThreshold");

            if (bestPersonId == null ||
                bestSimilarity < threshold)
            {
                return new RecognitionResponse
                {
                    Success = false,
                    PersonId = null,
                    Similarity = bestSimilarity,
                    Message = "No matching person found."
                };
            }

            return new RecognitionResponse
            {
                Success = true,
                PersonId = bestPersonId,
                EmployeeName = bestEmployeeName,
                Similarity = bestSimilarity,
                Message = "Person recognized."
            };
        }
    }
}
