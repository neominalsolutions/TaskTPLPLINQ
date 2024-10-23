using AsyncPrograming.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAsyncRequest, AsyncRequest>();

// DbContext Instance => Ayný instance üzerinden taþýnmasýný saðlar.
// Controller => Service => repository

var app = builder.Build();


// Her request de buraya girilecek.
app.Use(async (context, next) =>
{
  await Console.Out.WriteLineAsync($"Main Thread: {Thread.CurrentThread.ManagedThreadId}");
  await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
