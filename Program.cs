using maproute_simulation_SignalR_1.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();


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
          .WithOrigins("http://127.0.0.1:7233")
          .AllowCredentials(); // Allow credentials for the specified origins
});


app.MapHub<MapHub>("/maphub");


app.Run();
