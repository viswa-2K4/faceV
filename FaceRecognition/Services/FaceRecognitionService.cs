using FaceRecognition.Helpers;
using FaceRecognition.Models;

namespace FaceRecognition.Services;

public class FaceRecognitionService
{
    private readonly FaceModelService _model;
    private readonly EmployeeFaceCacheService _faceCache;
    private readonly IConfiguration _configuration;

    public FaceRecognitionService(
        FaceModelService model,
        EmployeeFaceCacheService faceCache,
        IConfiguration configuration)
    {
        _model = model;
        _faceCache = faceCache;
        _configuration = configuration;
    }

    // ============================================================
    // RECOGNIZE EMPLOYEE
    // ============================================================

    public async Task<RecognitionResponse> RecognizeAsync(
        IFormFile image,
        int industryId = 1)
    {
        // ========================================================
        // VALIDATE IMAGE
        // ========================================================

        if (image == null || image.Length == 0)
        {
            return new RecognitionResponse
            {
                Success = false,
                PersonId = null,
                EmployeeName = null,
                Similarity = 0,
                Message = "Image is required."
            };
        }

        // ========================================================
        // IMAGE -> EMBEDDING
        // ========================================================

        var inputEmbedding =
            await _model.GenerateEmbeddingAsync(image);

        // ========================================================
        // GET EMPLOYEES FROM HRMS FACE CACHE
        // ========================================================

        var employees =
            _faceCache.GetEmployees(industryId);

        if (employees == null || employees.Count == 0)
        {
            return new RecognitionResponse
            {
                Success = false,
                PersonId = null,
                EmployeeName = null,
                Similarity = 0,
                Message =
                    "Employee face cache is empty. Refresh the cache first."
            };
        }

        // ========================================================
        // RECOGNITION SETTINGS
        // ========================================================

        var threshold =
            _configuration.GetValue<float>(
                "FaceRecognition:SimilarityThreshold");

        var minimumMargin =
            _configuration.GetValue<float>(
                "FaceRecognition:MinimumMargin");

        // ========================================================
        // FIND BEST MATCH FROM HRMS EMPLOYEES
        // ========================================================

        CachedEmployeeFace? bestEmployee = null;

        float bestSimilarity = -1;

        float secondBestSimilarity = -1;

        foreach (var employee in employees)
        {
            if (employee.Embedding == null ||
                employee.Embedding.Length == 0)
            {
                Console.WriteLine(
                    $"[FACE MATCH] SKIPPED | " +
                    $"EmployeeId={employee.EmployeeId} | " +
                    $"Code={employee.EmployeeCode} | " +
                    $"Name={employee.EmployeeName} | " +
                    $"Reason=Empty embedding");

                continue;
            }

            var similarity =
                SimilarityHelper.CosineSimilarity(
                    inputEmbedding,
                    employee.Embedding);

            Console.WriteLine(
                $"[FACE MATCH] " +
                $"EmployeeId={employee.EmployeeId}, " +
                $"Code={employee.EmployeeCode}, " +
                $"Name={employee.EmployeeName}, " +
                $"Similarity={similarity:F6}");

            if (similarity > bestSimilarity)
            {
                secondBestSimilarity = bestSimilarity;
                bestSimilarity = similarity;
                bestEmployee = employee;
            }
            else if (similarity > secondBestSimilarity)
            {
                secondBestSimilarity = similarity;
            }
        }

        // ========================================================
        // NO VALID EMPLOYEE EMBEDDINGS
        // ========================================================

        if (bestEmployee == null)
        {
            return new RecognitionResponse
            {
                Success = false,
                PersonId = null,
                EmployeeName = null,
                Similarity = 0,
                Message = "No employee face available for recognition."
            };
        }

        // ========================================================
        // CALCULATE MARGIN
        // ========================================================

        var margin =
            secondBestSimilarity < 0
                ? bestSimilarity
                : bestSimilarity - secondBestSimilarity;

        Console.WriteLine(
            $"[FACE MATCH] " +
            $"BestEmployeeId={bestEmployee.EmployeeId} | " +
            $"BestCode={bestEmployee.EmployeeCode} | " +
            $"BestName={bestEmployee.EmployeeName} | " +
            $"BestSimilarity={bestSimilarity:F6} | " +
            $"SecondBestSimilarity={secondBestSimilarity:F6} | " +
            $"Margin={margin:F6} | " +
            $"Threshold={threshold:F6} | " +
            $"MinimumMargin={minimumMargin:F6}");

        // ========================================================
        // BELOW THRESHOLD
        // ========================================================

        if (bestSimilarity < threshold)
        {
            Console.WriteLine(
                $"[FACE MATCH] BELOW THRESHOLD | " +
                $"HRMS EmployeeId={bestEmployee.EmployeeId} | " +
                $"Code={bestEmployee.EmployeeCode} | " +
                $"Name={bestEmployee.EmployeeName} | " +
                $"Similarity={bestSimilarity:F6}");

            return new RecognitionResponse
            {
                Success = false,

                // KEEP HRMS EMPLOYEE INFORMATION
                PersonId = bestEmployee.EmployeeId,

                EmployeeName = bestEmployee.EmployeeName,

                Similarity = bestSimilarity,

                Message =
                    "HRMS employee found, but face similarity is below the recognition threshold."
            };
        }

        // ========================================================
        // AMBIGUOUS MATCH
        // ========================================================

        if (margin < minimumMargin)
        {
            Console.WriteLine(
                $"[FACE MATCH] AMBIGUOUS | " +
                $"HRMS EmployeeId={bestEmployee.EmployeeId} | " +
                $"Code={bestEmployee.EmployeeCode} | " +
                $"Name={bestEmployee.EmployeeName} | " +
                $"Similarity={bestSimilarity:F6} | " +
                $"Margin={margin:F6}");

            return new RecognitionResponse
            {
                Success = false,

                // KEEP HRMS EMPLOYEE INFORMATION
                PersonId = bestEmployee.EmployeeId,

                EmployeeName = bestEmployee.EmployeeName,

                Similarity = bestSimilarity,

                Message =
                    "HRMS employee found, but face match is ambiguous."
            };
        }

        // ========================================================
        // MATCH ACCEPTED
        // ========================================================

        Console.WriteLine(
            $"[FACE MATCH] MATCH ACCEPTED | " +
            $"EmployeeId={bestEmployee.EmployeeId} | " +
            $"Code={bestEmployee.EmployeeCode} | " +
            $"Name={bestEmployee.EmployeeName} | " +
            $"Similarity={bestSimilarity:F6}");

        // ========================================================
        // RETURN HRMS EMPLOYEE
        // ========================================================

        return new RecognitionResponse
        {
            Success = true,

            PersonId = bestEmployee.EmployeeId,

            EmployeeName = bestEmployee.EmployeeName,

            Similarity = bestSimilarity,

            Message = "Employee recognized."
        };
    }
}