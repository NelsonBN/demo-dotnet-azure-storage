using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace Demo.Api;

public class ImageEntity : Image, ITableEntity
{
    public string PartitionKey { get => Id.ToString(); set { } }
    public string RowKey { get => Name; set { } }
    public DateTimeOffset? Timestamp { get => CreatedAt; set { } }
    public ETag ETag { get; set; }
}

public class Image
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string FileName => $"{Name}{Extension}";
    public string Extension { get; set; } = default!;
    public string UniqueName { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = default!;

    public string ToJson()
        => JsonSerializer.Serialize(this);

    public override string ToString()
        => $"{nameof(Id)}: {Id}, {nameof(FileName)}: {FileName}, {nameof(Extension)}: {Extension}, {nameof(UniqueName)}: {UniqueName}, {nameof(CreatedAt)}: {CreatedAt}";

    public static Image Create(string fileName)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        return new()
        {
            Id = id,
            Name = fileName.Replace(extension, ""),
            Extension = extension,
            UniqueName = $"{id}-{fileName}",
            CreatedAt = DateTime.UtcNow,
        };
    }

    public ImageEntity To()
        => new()
        {
            Id = Id,
            Name = Name,
            Extension = Extension,
            UniqueName = UniqueName,
            CreatedAt = CreatedAt,
        };
}

public record ImageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public string UniqueName { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = default!;

    public static implicit operator ImageResponse(Image file)
        => new()
        {
            Id = file.Id,
            Name = file.Name,
            FileName = file.FileName,
            Extension = file.Extension,
            UniqueName = file.UniqueName,
            CreatedAt = file.CreatedAt,
        };
}
