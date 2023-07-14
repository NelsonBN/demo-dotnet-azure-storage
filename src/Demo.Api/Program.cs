using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Demo.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

builder.Services
    .AddSingleton(sp
        => new QueueClient(
            builder.Configuration.GetConnectionString("QueueStorage")!,
            "demo-image-queue"))
    .AddSingleton(sp
        => new BlobContainerClient(
            builder.Configuration.GetConnectionString("BlobStorage")!,
            "demo-image-container"))
    .AddSingleton(sp
        => new TableClient(
            new Uri(builder.Configuration.GetConnectionString("TableStorage")!),
            "DemoImageTable",
            new TableSharedKeyCredential("devstoreaccount1", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==")));

builder.Services.AddHostedService<Worker>();

var app = builder.Build();


app.UseSwagger()
   .UseSwaggerUI();

app.AddEndpoints();

app.Run();
