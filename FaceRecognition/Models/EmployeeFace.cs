namespace FaceRecognition.Models
{
    public class EmployeeFace
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = "";

        public string EmployeeCode { get; set; } = "";

        public string FaceImage { get; set; } = "";

        public float[] Embedding { get; set; } = [];
    }
}
