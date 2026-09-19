namespace FaceRecognition.Models
{
    public class EmployeeFaceResponse
    {
        public bool Success { get; set; }

        public List<EmployeeFace> Data { get; set; } = [];
    }
}
