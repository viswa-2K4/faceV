using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace FaceRecognition.Services;

public class FaceModelService
{
    private readonly InferenceSession _session;
    private readonly FaceDetectionService _detection;

    public FaceModelService(
        IWebHostEnvironment environment,
        FaceDetectionService detection)
    {
        _detection = detection;

        var modelPath = Path.Combine(
            environment.ContentRootPath,
            "FaceModels",
            "face-recognition.onnx");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Face recognition model not found.",
                modelPath);
        }

        _session = new InferenceSession(modelPath);

        Console.WriteLine($"Face model loaded: {modelPath}");
    }

    public async Task<float[]> GenerateEmbeddingAsync(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            throw new Exception("Image is required.");
        }

        await using var stream = image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        using var mat = Cv2.ImDecode(bytes, ImreadModes.Color);

        if (mat.Empty())
        {
            throw new Exception("Unable to read image.");
        }

        // ── Detect the face first — embedding models expect a tight,
        // aligned face crop, not a full photo squished to 112x112 ──
        var detection = _detection.Detect(mat);

        if (!detection.Success)
        {
            throw new Exception("No face detected in image.");
        }

        // ── Crop to the detected box with a bit of padding, clamped
        // to stay inside the original image bounds ──
        const float padding = 0.25f; // 25% padding around the box

        int boxX = (int)detection.X;
        int boxY = (int)detection.Y;
        int boxW = (int)detection.Width;
        int boxH = (int)detection.Height;

        int padX = (int)(boxW * padding);
        int padY = (int)(boxH * padding);

        int cropX = Math.Max(0, boxX - padX);
        int cropY = Math.Max(0, boxY - padY);
        int cropRight = Math.Min(mat.Width, boxX + boxW + padX);
        int cropBottom = Math.Min(mat.Height, boxY + boxH + padY);

        int cropW = cropRight - cropX;
        int cropH = cropBottom - cropY;

        if (cropW <= 0 || cropH <= 0)
        {
            throw new Exception("Invalid face crop region.");
        }

        using var faceCrop = new Mat(
            mat,
            new OpenCvSharp.Rect(cropX, cropY, cropW, cropH));

        Console.WriteLine(
            $"Face crop: {cropW} x {cropH} (from detected {boxW} x {boxH})");

        // ── Resize the FACE CROP (not the full photo) to 112x112 ──
        using var resized = new Mat();
        Cv2.Resize(faceCrop, resized, new OpenCvSharp.Size(112, 112));

        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        var tensor = new DenseTensor<float>(new[] { 1, 112, 112, 3 });

        for (int y = 0; y < 112; y++)
        {
            for (int x = 0; x < 112; x++)
            {
                Vec3b pixel = rgb.At<Vec3b>(y, x);

                tensor[0, y, x, 0] = (pixel.Item0 - 127.5f) / 128.0f;
                tensor[0, y, x, 1] = (pixel.Item1 - 127.5f) / 128.0f;
                tensor[0, y, x, 2] = (pixel.Item2 - 127.5f) / 128.0f;
            }
        }

        var inputName = _session.InputMetadata.Keys.First();
        var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        using var results = _session.Run(inputs);
        var output = results.First();
        var embedding = output.AsEnumerable<float>().ToArray();

        double sum = 0;
        foreach (var value in embedding) sum += value * value;
        var magnitude = Math.Sqrt(sum);

        if (magnitude == 0)
        {
            throw new Exception("Invalid face embedding.");
        }

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        Console.WriteLine($"Embedding size: {embedding.Length}");

        return embedding;
    }

    public async Task<float[]> GenerateEmbeddingFromStreamAsync(
    Stream stream)
    {
        using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        var bytes =
            memoryStream.ToArray();

        using var mat =
            Cv2.ImDecode(
                bytes,
                ImreadModes.Color);

        if (mat.Empty())
        {
            throw new Exception(
                "Unable to read face image.");
        }

        return GenerateEmbedding(mat);
    }


    public async Task<float[]> GenerateEmbeddingFromBytesAsync(
    byte[] imageBytes)
    {
        if (imageBytes == null ||
            imageBytes.Length == 0)
        {
            throw new Exception(
                "Image data is empty.");
        }

        using var mat =
            Cv2.ImDecode(
                imageBytes,
                ImreadModes.Color);

        if (mat.Empty())
        {
            throw new Exception(
                "Unable to decode image.");
        }

        return GenerateEmbedding(mat);
    }


    private float[] GenerateEmbedding(Mat mat)
    {
        var detection =
            _detection.Detect(mat);

        if (!detection.Success)
        {
            throw new Exception(
                "No face detected.");
        }

        // ============================================================
        // FACE CROP
        // ============================================================

        const float padding = 0.25f;

        int boxX = (int)detection.X;
        int boxY = (int)detection.Y;
        int boxW = (int)detection.Width;
        int boxH = (int)detection.Height;

        int padX = (int)(boxW * padding);
        int padY = (int)(boxH * padding);

        int cropX =
            Math.Max(0, boxX - padX);

        int cropY =
            Math.Max(0, boxY - padY);

        int cropRight =
            Math.Min(
                mat.Width,
                boxX + boxW + padX);

        int cropBottom =
            Math.Min(
                mat.Height,
                boxY + boxH + padY);

        int cropW =
            cropRight - cropX;

        int cropH =
            cropBottom - cropY;

        if (cropW <= 0 || cropH <= 0)
        {
            throw new Exception(
                "Invalid face crop.");
        }

        using var faceCrop =
            new Mat(
                mat,
                new OpenCvSharp.Rect(
                    cropX,
                    cropY,
                    cropW,
                    cropH));

        // ============================================================
        // RESIZE
        // ============================================================

        using var resized =
            new Mat();

        Cv2.Resize(
            faceCrop,
            resized,
            new OpenCvSharp.Size(
                112,
                112));

        // ============================================================
        // BGR -> RGB
        // ============================================================

        using var rgb =
            new Mat();

        Cv2.CvtColor(
            resized,
            rgb,
            ColorConversionCodes.BGR2RGB);

        // ============================================================
        // TENSOR
        // ============================================================

        var tensor =
            new DenseTensor<float>(
                new[] { 1, 112, 112, 3 });

        for (int y = 0; y < 112; y++)
        {
            for (int x = 0; x < 112; x++)
            {
                Vec3b pixel =
                    rgb.At<Vec3b>(y, x);

                tensor[0, y, x, 0] =
                    (pixel.Item0 - 127.5f) / 128.0f;

                tensor[0, y, x, 1] =
                    (pixel.Item1 - 127.5f) / 128.0f;

                tensor[0, y, x, 2] =
                    (pixel.Item2 - 127.5f) / 128.0f;
            }
        }

        // ============================================================
        // ONNX
        // ============================================================

        var inputName =
            _session
                .InputMetadata
                .Keys
                .First();

        var inputs =
            new[]
            {
            NamedOnnxValue.CreateFromTensor(
                inputName,
                tensor)
            };

        using var results =
            _session.Run(inputs);

        var output =
            results.First();

        var embedding =
            output
                .AsEnumerable<float>()
                .ToArray();

        // ============================================================
        // L2 NORMALIZATION
        // ============================================================

        double sum = 0;

        foreach (var value in embedding)
        {
            sum += value * value;
        }

        double magnitude =
            Math.Sqrt(sum);

        if (magnitude == 0)
        {
            throw new Exception(
                "Invalid face embedding.");
        }

        for (int i = 0;
             i < embedding.Length;
             i++)
        {
            embedding[i] =
                (float)(
                    embedding[i] /
                    magnitude);
        }

        return embedding;
    }


}