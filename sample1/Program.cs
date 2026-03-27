using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace sample1;

class Personne
{
    public string nom { get; set; } = string.Empty;
    public int age { get; set; }

    public string Hello(bool isLowercase)
    {
        var message = $"hello {nom}, you are {age}";
        return isLowercase ? message : message.ToUpperInvariant();
    }
}

class Program
{
    static void Main(string[] args)
    {
        var personne = new Personne
        {
            nom = "Alice",
            age = 22
        };

        var json = JsonConvert.SerializeObject(personne, Formatting.Indented);
        Console.WriteLine(json);

        // Image source: argument 1, sinon "input.jpg" dans le dossier courant.
        var inputPath = args.Length > 0 ? args[0] : "input.jpg";
        var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "resized.jpg");

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Image introuvable: {inputPath}");
            Console.WriteLine("Astuce: passez le chemin en argument, ex: dotnet run -- ./mon-image.jpg");
            return;
        }

        using (var image = Image.Load(inputPath))
        {
            image.Mutate(x => x.Resize(300, 300));
            image.SaveAsJpeg(outputPath);
        }

        Console.WriteLine($"Image redimensionnee en 300x300 et sauvegardee ici: {outputPath}");
    }
}
