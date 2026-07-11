using Rushframe.Domain;
using Rushframe.Domain.Serialization;

namespace Rushframe.Infrastructure;

public sealed class AutosaveService
{
    private readonly string _autosaveDir;
    private const int MaxKeep = 10;
    private CancellationTokenSource? _cts;

    public AutosaveService(string autosaveDir)
    {
        _autosaveDir = autosaveDir;
        Directory.CreateDirectory(autosaveDir);
    }

    public void Save(Project project)
    {
        var temp = Path.Combine(_autosaveDir, $".{project.Id}.tmp");
        var final = Path.Combine(_autosaveDir, $"{project.Id}.autosave");
        var json = ProjectSerializer.Serialize(project);
        File.WriteAllText(temp, json);
        File.Move(temp, final, overwrite: true);

        foreach (var f in Directory.GetFiles(_autosaveDir, "*.autosave")
                       .OrderByDescending(File.GetLastWriteTime).Skip(MaxKeep))
            File.Delete(f);
    }

    public Project? LoadMostRecent()
    {
        var latest = Directory.GetFiles(_autosaveDir, "*.autosave")
            .OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
        return latest != null ? ProjectSerializer.Deserialize(File.ReadAllText(latest)) : null;
    }

    public void StartBackground(Project project, TimeSpan interval)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, token);
                if (!token.IsCancellationRequested) Save(project);
            }
        }, token);
    }

    public void StopBackground()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
