using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZgodnieZTutorialem.Components.DatabaseAccess;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTransient<SqlDataAccess>();
builder.Services.AddTransient<LeaderboardData>();

await builder.Build().RunAsync();
