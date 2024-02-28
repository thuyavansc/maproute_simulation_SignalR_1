using maproute_simulation_SignalR_1.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//NOTE::::::: COMMENT THIS LINE TO WORK::::::
//app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();
// Enable CORS for the "/chathub" endpoint
app.UseCors(policy =>
{
    policy.AllowAnyMethod()
          .AllowAnyHeader()
          //.AllowAnyOrigin()
          //.WithOrigins("https://localhost:7233", "http://192.168.56.1:7233", "http://192.168.8.157:7233", "https://192.168.56.1:7233", "https://192.168.8.157:7233") // Add your specific origins here
          //.WithOrigins("https://localhost:7233", "http://192.168.8.157:7233", "http://192.168.8.157:7233", "https://192.168.56.1:7233", "https://192.168.8.157:7233") // Add your specific origins here
          //.WithOrigins("https://localhost:7233", "http://192.168.56.1:7233")
          .WithOrigins("http://127.0.0.1:7233")
          .AllowCredentials(); // Allow credentials for the specified origins
});


app.MapHub<MapHub>("/maphub");


app.Run();
