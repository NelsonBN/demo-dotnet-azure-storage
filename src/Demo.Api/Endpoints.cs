using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;

namespace Demo.Api;

public static class Endpoints
{
    public static void AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("images");

        group.MapPost(
            "",
            async (QueueClient queue, BlobContainerClient blob, TableClient table, IFormFile request, CancellationToken cancellationToken) =>
            {
                // Define file metadata
                var file = Image.Create(request.FileName);


                // Upload file to blob storage
                var blobClient = blob.GetBlobClient(file.UniqueName);
                var blobResponse = await blobClient.UploadAsync(
                    request.OpenReadStream(),
                    new BlobHttpHeaders { ContentType = request.ContentType },
                    new Dictionary<string, string>
                    {
                        [nameof(Image.Id)] = file.Id.ToString(),
                        [nameof(Image.FileName)] = file.FileName,
                        [nameof(Image.Extension)] = file.Extension,
                        [nameof(Image.CreatedAt)] = file.CreatedAt.ToString(),
                    },
                    cancellationToken: cancellationToken);

                var httpBlobResponse = blobResponse.GetRawResponse();
                if(httpBlobResponse.Status != (int)HttpStatusCode.Created)
                {
                    throw new InvalidOperationException(
                        $"Was not possible to upload the file to the blob storage. HttpStatusCode: '{httpBlobResponse?.Status}', ReasonPhrase: '{httpBlobResponse?.ReasonPhrase}'",
                        new Exception(JsonSerializer.Serialize(httpBlobResponse)));
                }

                // Add message to queue
                var queueResponse = await queue.SendMessageAsync(
                    file.ToJson(),
                    cancellationToken);

                var httpQueueResponse = queueResponse.GetRawResponse();
                if(httpQueueResponse.Status != (int)HttpStatusCode.Created)
                {
                    throw new InvalidOperationException(
                        $"Was not possible to add the message to the queue. HttpStatusCode: '{httpQueueResponse?.Status}', ReasonPhrase: '{httpQueueResponse?.ReasonPhrase}'",
                        new Exception(JsonSerializer.Serialize(httpQueueResponse)));
                }


                // Add item in table storage
                await table.AddEntityAsync(
                    file.To(),
                    cancellationToken);

                return Results.Created($"{blob.Uri.AbsoluteUri}/{file.UniqueName}", (ImageResponse)file);
            });

        group.MapGet(
            "",
            (TableClient table, CancellationToken cancellationToken) =>
            {
                var results = table.Query<ImageEntity>(q => q.FileName != "", cancellationToken: cancellationToken);

                return Results.Ok(results.Select(s => (ImageResponse)s));
            });

        group.MapGet(
            "{id:guid}",
            (TableClient table, Guid id, CancellationToken cancellationToken) =>
            {
                var results = table.Query<ImageEntity>(q => q.Id == id, cancellationToken: cancellationToken);

                var file = results.SingleOrDefault();
                if(file is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok((ImageResponse)file);
            });

        group.MapGet(
            "files",
            (BlobContainerClient blob, CancellationToken cancellationToken) =>
            {
                var files = blob.GetBlobs(cancellationToken: cancellationToken);
                return Results.Ok(files.Select(s => $"{blob.Uri.AbsoluteUri}/{s.Name}"));
            });

        group.MapDelete(
            "{id:guid}",
            async (BlobContainerClient blob, TableClient table, Guid id, CancellationToken cancellationToken) =>
            {
                var results = table.Query<ImageEntity>(q => q.Id == id, cancellationToken: cancellationToken);

                var file = results.SingleOrDefault();
                if(file is null)
                {
                    return Results.NotFound();
                }

                await table.DeleteEntityAsync(file.PartitionKey, file.Name, cancellationToken: cancellationToken);

                var response = await blob.DeleteBlobAsync(file.UniqueName, cancellationToken: cancellationToken);

                if(response.Status != (int)HttpStatusCode.Accepted)
                {
                    throw new InvalidOperationException($"Was not possible to delete the file from the blob storage. HttpStatusCode: '{response?.Status}', ReasonPhrase: '{response?.ReasonPhrase}'");
                }

                return Results.NoContent();
            });
    }
}
