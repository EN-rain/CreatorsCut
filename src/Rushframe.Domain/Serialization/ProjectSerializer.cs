using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rushframe.Domain.Serialization;

public static class ProjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new MediaTimeConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    public static string Serialize(Project project)
        => JsonSerializer.Serialize(project, Options);

    public static Project Deserialize(string json)
        => JsonSerializer.Deserialize<Project>(json, Options)
           ?? throw new InvalidOperationException("Deserialized project is null");
}
