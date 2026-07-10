using CreatorCut.Domain;
using CreatorCut.Domain.Serialization;
using CreatorCut.LegacyImport;

namespace CreatorCut.Application;

public sealed class MigrationResult
{
    public bool Success { get; set; }
    public Project? Project { get; set; }
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public string BackupPath { get; set; } = "";
}

public sealed class MigrationService
{
    private readonly string _backupRoot;

    public MigrationService(string backupRoot)
    {
        _backupRoot = backupRoot;
        Directory.CreateDirectory(backupRoot);
    }

    public MigrationResult MigrateLegacyProject(string legacyProjectPath)
    {
        var result = new MigrationResult();

        try
        {
            var importer = new LegacyImporter();
            var importResult = importer.ImportProject(legacyProjectPath);

            result.Warnings.AddRange(importResult.Warnings);
            result.Errors.AddRange(importResult.Errors);

            if (importResult.Project == null)
            {
                result.Success = false;
                return result;
            }

            result.Project = importResult.Project;
            result.BackupPath = CreateBackup(legacyProjectPath);

            var desktopPath = Path.ChangeExtension(legacyProjectPath, ".creatorcut");
            var json = ProjectSerializer.Serialize(importResult.Project);
            File.WriteAllText(desktopPath, json);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Success = false;
        }

        return result;
    }

    private string CreateBackup(string originalPath)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupDir = Path.Combine(_backupRoot, $"{Path.GetFileNameWithoutExtension(originalPath)}_{timestamp}");
        Directory.CreateDirectory(backupDir);

        var sourceDir = Path.GetDirectoryName(originalPath) ?? ".";
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(backupDir, Path.GetFileName(file)), overwrite: false);

        return backupDir;
    }

    public List<string> ListBackups(string originalProjectName)
    {
        return Directory.GetDirectories(_backupRoot, $"{originalProjectName}_*")
            .OrderByDescending(Directory.GetCreationTime)
            .ToList();
    }

    public bool RestoreBackup(string backupPath, string targetPath)
    {
        if (!Directory.Exists(backupPath)) return false;

        foreach (var file in Directory.GetFiles(backupPath))
        {
            var dest = Path.Combine(Path.GetDirectoryName(targetPath) ?? ".", Path.GetFileName(file));
            if (!File.Exists(dest))
                File.Copy(file, dest);
        }

        return true;
    }
}
