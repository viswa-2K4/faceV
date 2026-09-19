namespace FaceRecognition.Helpers
{
    public class SimilarityHelper
    {
        public static float CosineSimilarity(
        float[] a,
        float[] b)
        {
            if (a.Length != b.Length)
                throw new Exception(
                    "Embedding dimensions are different.");

            double dot = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];

                magnitudeA += a[i] * a[i];

                magnitudeB += b[i] * b[i];
            }

            if (magnitudeA == 0 ||
                magnitudeB == 0)
            {
                return 0;
            }

            return (float)(
                dot /
                (Math.Sqrt(magnitudeA) *
                 Math.Sqrt(magnitudeB))
            );
        }
    }
}
