using BedRock.POC.API.Endpoints;
using BedRock.POC.API.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext ctx) =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    return app.Environment.IsDevelopment() && ex != null
        ? Results.Problem(detail: ex.ToString(), statusCode: 500)
        : Results.Problem(statusCode: 500);
});

app.MapDocumentEndpoints();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Program { }
