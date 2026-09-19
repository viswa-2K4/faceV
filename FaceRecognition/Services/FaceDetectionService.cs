using FaceRecognition.Models;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace FaceRecognition.Services;

public class FaceDetectionService
{
    private readonly FaceDetectorYN _detector;

    public FaceDetectionService(
        IWebHostEnvironment environment)
    {
        var modelPath = Path.Combine(
            environment.ContentRootPath,
            "FaceModels",
            "face-detection.onnx");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Face detection model not found.",
                modelPath);
        }

        _detector = FaceDetectorYN.Create(
            modelPath,
            "",
            new OpenCvSharp.Size(320, 320),
            0.6f,
            0.3f,
            5000,
            Backend.OPENCV,
            Target.CPU);

        Console.WriteLine(
            $"Face detection model loaded: {modelPath}");
    }


    public async Task<FaceDetectionResult> DetectAsync(
        IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            throw new Exception(
                "Image is required.");
        }


        // -----------------------------
        // Read uploaded image
        // -----------------------------

        await using var stream =
            image.OpenReadStream();

        using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(
            memoryStream);

        var imageBytes =
            memoryStream.ToArray();


        using var original =
            Cv2.ImDecode(
                imageBytes,
                ImreadModes.Color);


        if (original.Empty())
        {
            throw new Exception(
                "Unable to read image.");
        }


        Console.WriteLine(
            $"Original image: " +
            $"{original.Width} x {original.Height}");


        // -----------------------------
        // YuNet requires 320 x 320
        // -----------------------------

        using var resized =
            new Mat();

        Cv2.Resize(
            original,
            resized,
            new OpenCvSharp.Size(
                320,
                320));


        Console.WriteLine(
            $"Detection image: " +
            $"{resized.Width} x {resized.Height}");


        // -----------------------------
        // Detect face
        // -----------------------------

        using var faces =
            new Mat();


        _detector.Detect(
            resized,
            faces);


        if (faces.Empty() ||
            faces.Rows == 0)
        {
            return new FaceDetectionResult
            {
                Success = false,
                Message = "No face detected."
            };
        }


        // -----------------------------
        // Find highest confidence face
        // -----------------------------

        int bestIndex = 0;

        float bestScore = -1;


        for (int i = 0; i < faces.Rows; i++)
        {
            float score =
                faces.At<float>(
                    i,
                    14);


            Console.WriteLine(
                $"Face {i}: " +
                $"confidence={score:F4}");


            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }


        // -----------------------------
        // YuNet output
        // -----------------------------

        float x =
            faces.At<float>(
                bestIndex,
                0);

        float y =
            faces.At<float>(
                bestIndex,
                1);

        float width =
            faces.At<float>(
                bestIndex,
                2);

        float height =
            faces.At<float>(
                bestIndex,
                3);


        float rightEyeX =
            faces.At<float>(
                bestIndex,
                4);

        float rightEyeY =
            faces.At<float>(
                bestIndex,
                5);


        float leftEyeX =
            faces.At<float>(
                bestIndex,
                6);

        float leftEyeY =
            faces.At<float>(
                bestIndex,
                7);


        float noseX =
            faces.At<float>(
                bestIndex,
                8);

        float noseY =
            faces.At<float>(
                bestIndex,
                9);


        float rightMouthX =
            faces.At<float>(
                bestIndex,
                10);

        float rightMouthY =
            faces.At<float>(
                bestIndex,
                11);


        float leftMouthX =
            faces.At<float>(
                bestIndex,
                12);

        float leftMouthY =
            faces.At<float>(
                bestIndex,
                13);


        Console.WriteLine(
            $"Face detected: " +
            $"X={x:F1}, " +
            $"Y={y:F1}, " +
            $"W={width:F1}, " +
            $"H={height:F1}, " +
            $"Confidence={bestScore:F4}");


        // -----------------------------
        // Convert coordinates back
        // to original image size
        // -----------------------------

        float scaleX =
            (float)original.Width / 320f;

        float scaleY =
            (float)original.Height / 320f;


        return new FaceDetectionResult
        {
            Success = true,

            X = x * scaleX,
            Y = y * scaleY,

            Width = width * scaleX,
            Height = height * scaleY,


            RightEyeX =
                rightEyeX * scaleX,

            RightEyeY =
                rightEyeY * scaleY,


            LeftEyeX =
                leftEyeX * scaleX,

            LeftEyeY =
                leftEyeY * scaleY,


            NoseX =
                noseX * scaleX,

            NoseY =
                noseY * scaleY,


            RightMouthX =
                rightMouthX * scaleX,

            RightMouthY =
                rightMouthY * scaleY,


            LeftMouthX =
                leftMouthX * scaleX,

            LeftMouthY =
                leftMouthY * scaleY,


            Confidence =
                bestScore,

            Message =
                "Face detected."
        };
    }


    public FaceDetectionResult Detect(Mat image)
    {
        if (image == null || image.Empty())
        {
            return new FaceDetectionResult
            {
                Success = false,
                Message = "Invalid image."
            };
        }

        Console.WriteLine(
            $"Detection image: {image.Width} x {image.Height}");

        // -----------------------------
        // Resize to YuNet input
        // -----------------------------

        using var resized = new Mat();

        Cv2.Resize(
            image,
            resized,
            new OpenCvSharp.Size(320, 320));

        // -----------------------------
        // Detect faces
        // -----------------------------

        using var faces = new Mat();

        _detector.Detect(
            resized,
            faces);

        if (faces.Empty() || faces.Rows == 0)
        {
            return new FaceDetectionResult
            {
                Success = false,
                Message = "No face detected."
            };
        }

        // -----------------------------
        // Find highest confidence face
        // -----------------------------

        int bestIndex = 0;
        float bestScore = -1;

        for (int i = 0; i < faces.Rows; i++)
        {
            float score = faces.At<float>(i, 14);

            Console.WriteLine(
                $"Face {i}: confidence={score:F4}");

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // -----------------------------
        // Bounding box
        // -----------------------------

        float x = faces.At<float>(bestIndex, 0);
        float y = faces.At<float>(bestIndex, 1);
        float width = faces.At<float>(bestIndex, 2);
        float height = faces.At<float>(bestIndex, 3);

        // -----------------------------
        // Facial landmarks
        // -----------------------------

        float rightEyeX = faces.At<float>(bestIndex, 4);
        float rightEyeY = faces.At<float>(bestIndex, 5);

        float leftEyeX = faces.At<float>(bestIndex, 6);
        float leftEyeY = faces.At<float>(bestIndex, 7);

        float noseX = faces.At<float>(bestIndex, 8);
        float noseY = faces.At<float>(bestIndex, 9);

        float rightMouthX = faces.At<float>(bestIndex, 10);
        float rightMouthY = faces.At<float>(bestIndex, 11);

        float leftMouthX = faces.At<float>(bestIndex, 12);
        float leftMouthY = faces.At<float>(bestIndex, 13);

        // -----------------------------
        // Convert coordinates back
        // to original image size
        // -----------------------------

        float scaleX = (float)image.Width / 320f;
        float scaleY = (float)image.Height / 320f;

        return new FaceDetectionResult
        {
            Success = true,

            X = x * scaleX,
            Y = y * scaleY,

            Width = width * scaleX,
            Height = height * scaleY,

            RightEyeX = rightEyeX * scaleX,
            RightEyeY = rightEyeY * scaleY,

            LeftEyeX = leftEyeX * scaleX,
            LeftEyeY = leftEyeY * scaleY,

            NoseX = noseX * scaleX,
            NoseY = noseY * scaleY,

            RightMouthX = rightMouthX * scaleX,
            RightMouthY = rightMouthY * scaleY,

            LeftMouthX = leftMouthX * scaleX,
            LeftMouthY = leftMouthY * scaleY,

            Confidence = bestScore,

            Message = "Face detected."
        };
    }


}