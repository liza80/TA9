using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using ServiceB;
using ServiceB.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddDbContext<GraphDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromMinutes(2);
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetService<GraphDbContext>();
    dbContext.Database.EnsureCreated();
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseWebSockets();

//app.Map("/ws", async context =>
//{
//    var dbContext = context.RequestServices.GetRequiredService<GraphDbContext>();
//    var handler = new WebSocketHandler(dbContext);
//    await handler.HandleAsync(context);
//});

app.MapControllers();

app.Run();

