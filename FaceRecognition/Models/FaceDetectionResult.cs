namespace FaceRecognition.Models
{
    public class FaceDetectionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "";

        public float Confidence { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public float RightEyeX { get; set; }

        public float RightEyeY { get; set; }

        public float LeftEyeX { get; set; }

        public float LeftEyeY { get; set; }

        public float NoseX { get; set; }

        public float NoseY { get; set; }

        public float RightMouthX { get; set; }

        public float RightMouthY { get; set; }

        public float LeftMouthX { get; set; }

        public float LeftMouthY { get; set; }
    }
}
