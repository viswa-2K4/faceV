using FaceRecognition.Data;
using FaceRecognition.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICES
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// DATABASE
// ============================================================

builder.Services.AddSingleton<MySqlConnectionFactory>();


// ============================================================
// HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient();


// ============================================================
// MEMORY CACHE
// ============================================================

builder.Services.AddMemoryCache();


// ============================================================
// FACE SERVICES
// ============================================================

// ONNX models are loaded once and reused.
builder.Services.AddSingleton<FaceDetectionService>();

builder.Services.AddSingleton<FaceModelService>();


// ============================================================
// EMPLOYEE FACE CACHE
// ============================================================

builder.Services.AddSingleton<EmployeeFaceCacheService>();

builder.Services.AddHostedService<EmployeeFaceCacheBackgroundService>();


// ============================================================
// OTHER SERVICES
// ============================================================

builder.Services.AddScoped<FaceRegistrationService>();

builder.Services.AddScoped<FaceRecognitionService>();


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FaceWeb", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();


// ============================================================
// MIDDLEWARE
// ============================================================

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("FaceWeb");

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();