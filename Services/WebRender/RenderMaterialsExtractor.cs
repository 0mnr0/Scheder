using System.Reflection;

namespace Scheder.Services.WebRender;

public static class RenderMaterialsExtractor
{
    public static string Extract()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var targetDir = Path.Combine(Path.GetTempPath(), "RenderMaterials_" + assembly.GetName().Version);

        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, recursive: true);

        Directory.CreateDirectory(targetDir);

        // Префикс зависит от default namespace проекта + пути папки.
        // Проверьте реальные имена через assembly.GetManifestResourceNames()
        const string prefix = "Scheder.Services.WebRender.RenderMaterials.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix)) continue;

            // resourceName вида: YourApp...RenderMaterials.css.style.css
            var relative = resourceName.Substring(prefix.Length);

            // Имена ресурсов "плоские" — точки вместо разделителей папок.
            // Последнюю точку нужно сохранить как расширение файла,
            // остальные точки в пути — заменить на разделитель директорий.
            var lastDot = relative.LastIndexOf('.');
            var ext = relative[lastDot..];
            var nameWithoutExt = relative[..lastDot].Replace('.', Path.DirectorySeparatorChar);
            var relativePath = nameWithoutExt + ext;

            var fullPath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var fileStream = File.Create(fullPath);
            stream.CopyTo(fileStream);
        }

        return targetDir;
    }
}