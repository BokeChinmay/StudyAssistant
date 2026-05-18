using StudyAssistant.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// builder.Services.AddSingleton(new AnthropicClient(
//     builder.Configuration["Anthropic:ApiKey"]!
// ));

builder.Services.AddHttpClient();

builder.Services.AddCors(options => {
    options.AddPolicy("BlazorClient", policy => policy.WithOrigins("http://localhost:5211").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ConversationService>();

var app = builder.Build();

app.UseCors("BlazorClient");
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
