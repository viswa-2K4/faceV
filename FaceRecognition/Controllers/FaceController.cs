using FaceRecognition.Models;
using FaceRecognition.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognition.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceController : ControllerBase
    {


        private readonly FaceRegistrationService
        _registration;

        private readonly FaceRecognitionService
            _recognition;

        private readonly FaceDetectionService _detection;

        private readonly EmployeeFaceCacheService _faceCache;

        public FaceController(
            FaceRegistrationService registration,
            FaceRecognitionService recognition, FaceDetectionService detection, EmployeeFaceCacheService faceCache)
        {
            _registration = registration;

            _recognition = recognition;
            _detection = detection;
            _faceCache = faceCache;
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register(
            [FromForm] RegisterFaceRequest request)
        {
            try
            {
                await _registration.RegisterAsync(
                    request.PersonId, request.EmployeeName,
                    request.Image);

                return Ok(new
                {
                    success = true,
                    personId = request.PersonId,
                    employeeName = request.EmployeeName,
                    message = "Face embedding stored successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("recognize")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Recognize(
            IFormFile image)
        {
            try
            {
                var result =
                    await _recognition.RecognizeAsync(
                        image);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("detect")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Detect(
    IFormFile image)
        {
            try
            {
                var result =
                    await _detection.DetectAsync(image);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // REFRESH EMPLOYEE FACE CACHE
        // ============================================================

        [HttpPost("cache/refresh")]
        public async Task<IActionResult> RefreshFaceCache(
            [FromQuery] int industryId = 1)
        {
            await _faceCache.RefreshAsync(industryId);

            var employees =
                _faceCache.GetEmployees(industryId);

            return Ok(new
            {
                success = true,
                message = "Employee face cache refreshed.",
                industryId,
                cachedEmployees = employees.Count
            });
        }
    }
}

