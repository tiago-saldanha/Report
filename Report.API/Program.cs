using Report.API.Endpoints;
using Report.API.Repository;
using Report.API.Services;
using Report.API.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReportRepository>();
builder.Services.AddScoped<ReportApplicationService>();
builder.Services.AddSingleton<SnapshotApplicationService>();
builder.Services.AddHostedService<SnapshotWorkerService>();
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