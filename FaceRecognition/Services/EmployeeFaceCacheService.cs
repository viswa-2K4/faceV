using System.Collections.Concurrent;
using System.Text.Json;

namespace FaceRecognition.Services;

public class EmployeeFaceCacheService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FaceModelService _faceModelService;

    private readonly ConcurrentDictionary<
        int,
        List<CachedEmployeeFace>> _cache = new();

    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmployeeFaceCacheService(
        IHttpClientFactory httpClientFactory,
        FaceModelService faceModelService)
    {
        _httpClientFactory = httpClientFactory;
        _faceModelService = faceModelService;
    }

    // ============================================================
    // REFRESH CACHE
    // ============================================================

    public async Task RefreshAsync(int industryId)
    {
        await _lock.WaitAsync();

        try
        {
            Console.WriteLine(
                $"[FACE CACHE] Checking industry {industryId}");

            var client =
                _httpClientFactory.CreateClient();

            var url =
                $"https://app-byposs-backend-linux-gmcvdvf6ecdsg5en.canadacentral-01.azurewebsites.net" +
                $"/api/hrms/v1/PunchControl/employee-face/industry/{industryId}";

            var response =
                await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content.ReadAsStringAsync();

            var apiResponse =
                JsonSerializer.Deserialize<EmployeeFaceApiResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (apiResponse?.Data == null)
            {
                Console.WriteLine(
                    "[FACE CACHE] No employee data.");

                return;
            }

            _cache.TryGetValue(
                industryId,
                out var oldCache);

            oldCache ??= [];

            var oldDictionary =
                oldCache.ToDictionary(
                    x => x.EmployeeId);

            var newCache =
                new List<CachedEmployeeFace>();

            foreach (var employee in apiResponse.Data)
            {
                // ====================================================
                // ALREADY CACHED + SAME IMAGE
                // ====================================================

                if (oldDictionary.TryGetValue(
                    employee.EmployeeId,
                    out var oldEmployee))
                {
                    if (oldEmployee.FaceImage ==
                        employee.FaceImage)
                    {
                        newCache.Add(oldEmployee);

                        Console.WriteLine(
                            $"[FACE CACHE] Unchanged: " +
                            $"{employee.EmployeeCode}");

                        continue;
                    }

                    Console.WriteLine(
                        $"[FACE CACHE] Image changed: " +
                        $"{employee.EmployeeCode}");
                }
                else
                {
                    Console.WriteLine(
                        $"[FACE CACHE] New employee: " +
                        $"{employee.EmployeeCode}");
                }

                // ====================================================
                // DOWNLOAD IMAGE
                // ====================================================

                try
                {
                    var imageBytes =
                        await client.GetByteArrayAsync(
                            employee.FaceImage);

                    var embedding =
                        await _faceModelService
                            .GenerateEmbeddingFromBytesAsync(
                                imageBytes);

                    newCache.Add(
                        new CachedEmployeeFace
                        {
                            EmployeeId =
                                employee.EmployeeId,

                            EmployeeName =
                                employee.EmployeeName,

                            EmployeeCode =
                                employee.EmployeeCode,

                            FaceImage =
                                employee.FaceImage,

                            Embedding =
                                embedding
                        });

                    Console.WriteLine(
                        $"[FACE CACHE] Cached: " +
                        $"{employee.EmployeeCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[FACE CACHE] Failed " +
                        $"{employee.EmployeeCode}: " +
                        $"{ex.Message}");

                    // Keep previous embedding if download fails
                    if (oldEmployee != null)
                    {
                        newCache.Add(oldEmployee);
                    }
                }
            }

            // Replace cache atomically
            _cache[industryId] = newCache;

            Console.WriteLine(
                $"[FACE CACHE] Finished. " +
                $"Cached employees: {newCache.Count}");
        }
        finally
        {
            _lock.Release();
        }
    }


    // ============================================================
    // GET CACHED EMPLOYEES
    // ============================================================

    public List<CachedEmployeeFace> GetEmployees(
        int industryId)
    {
        if (_cache.TryGetValue(
            industryId,
            out var employees))
        {
            return employees;
        }

        return [];
    }


    // ============================================================
    // CHECK CACHE
    // ============================================================

    public bool HasCache(int industryId)
    {
        return _cache.ContainsKey(industryId);
    }
}


// ================================================================
// HRMS API RESPONSE
// ================================================================

public class EmployeeFaceApiResponse
{
    public bool Success { get; set; }

    public List<EmployeeFaceDto> Data { get; set; } = [];
}


// ================================================================
// HRMS EMPLOYEE FACE
// ================================================================

public class EmployeeFaceDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = "";

    public string EmployeeCode { get; set; } = "";

    public string FaceImage { get; set; } = "";
}


// ================================================================
// CACHE OBJECT
// ================================================================

public class CachedEmployeeFace
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = "";

    public string EmployeeCode { get; set; } = "";

    public string FaceImage { get; set; } = "";

    public float[] Embedding { get; set; } = [];
}