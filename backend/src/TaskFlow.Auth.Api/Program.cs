using TaskFlow.Api.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.AddTaskFlow();
var app = builder.Build();
app.UseTaskFlow();
app.Run();

public partial class Program { }
