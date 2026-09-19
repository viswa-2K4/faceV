namespace FaceRecognition.Models
{
    public class RegisterFaceRequest
    {
        public long PersonId { get; set; }

        public string EmployeeName { get; set; } = "";
        public IFormFile Image { get; set; } = null!;
    }
}
