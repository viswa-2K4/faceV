namespace FaceRecognition.Models
{
    public class RecognitionResponse
    {
        public bool Success { get; set; }

        public long? PersonId { get; set; }

        public string? EmployeeName { get; set; }

        public float Similarity { get; set; }

        public string Message { get; set; } = "";
    }
}
