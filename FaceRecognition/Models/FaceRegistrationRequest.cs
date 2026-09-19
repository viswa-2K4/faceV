namespace FaceRecognition.Models
{
    public class FaceRegistrationRequest
    {
        public long EmployeeId { get; set; }

        public IFormFile Image { get; set; } = null!;
    }
}
