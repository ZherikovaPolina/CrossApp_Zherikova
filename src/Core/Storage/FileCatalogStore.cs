using System.Text.Json;
using System.Text.Encodings.Web;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore
{
    private readonly string _path;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public FileCatalogStore(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public IReadOnlyList<ProductDto> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        string json = File.ReadAllText(_path);

        return JsonSerializer.Deserialize<List<ProductDto>>(json)
            ?? [];
    }

    public void Save(IEnumerable<ProductDto> products)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_path)!);

        string tempPath = _path + ".tmp";

        string json = JsonSerializer.Serialize(
            products.ToList(),
            Options);

        try
        {
            File.WriteAllText(tempPath, json);

            File.Move(
                tempPath,
                _path,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
