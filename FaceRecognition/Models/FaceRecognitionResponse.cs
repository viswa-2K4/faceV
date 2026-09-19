namespace FaceRecognition.Models
{
    public class FaceRecognitionResponse
    {
        public bool Success { get; set; }

        public long? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public float Similarity { get; set; }

        public string Message { get; set; } = "";
    }
}
