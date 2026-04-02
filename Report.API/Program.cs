using Report.API.Services;
using Report.API.Endpoints;
using Scalar.AspNetCore;
using Report.API.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReportRepository>();
builder.Services.AddScoped<ReportApplicationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Report API")
               .WithClassicLayout()
               .ForceDarkMode()
               .HideSearch()
               .ShowOperationId()
               .ExpandAllTags()
               .SortTagsAlphabetically()
               .SortOperationsByMethod()
               .PreserveSchemaPropertyOrder();
    });
}

app.UseHttpsRedirection();

app.MapReportEndpoints();

app.Run();