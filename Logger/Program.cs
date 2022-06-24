using Logger.Middlewares;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Routing with lower cases
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.ResponsePropertiesAndHeaders;
    options.ResponseHeaders.Add("Non-Sensitive");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Inject Request-Response logger middleware
//app.UseMiddleware<LoggerMiddleware>();


app.UseHttpLogging();

app.UseAuthorization();

app.MapControllers();

app.Run();
