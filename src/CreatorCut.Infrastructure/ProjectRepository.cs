using CreatorCut.Domain;
using CreatorCut.Domain.Serialization;

namespace CreatorCut.Infrastructure;

public sealed class ProjectRepository
{
    public void Save(Project project, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var temp = path + ".tmp";
        var json = ProjectSerializer.Serialize(project);
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }

    public Project? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return ProjectSerializer.Deserialize(json);
    }

    public bool Exists(string path) => File.Exists(path);
}
