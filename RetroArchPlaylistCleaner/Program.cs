using System.Text.Json.Nodes;

JsonNode? jObject = null;
string? playlistPath;

do
{
    Console.Write("Enter Retroarch playlist directory: ");
    playlistPath = Console.ReadLine();

    if (File.Exists(playlistPath))
    {
        var json = await File.ReadAllTextAsync(playlistPath);

        if (!json.StartsWith('{'))
        {
            Console.Write("Error parsing playlist, please make sure you have selected a valid retroarch playlist which has not been compressed.");
            return;
        }

        jObject = JsonNode.Parse(json);
    }
    else
    {
        Console.WriteLine("Invalid directory, please try again.\n\n");
    }
}
while (jObject == null);

//Populate list of files in game directory.
var playlistGameDirectory = jObject["scan_content_dir"]?.ToString();

if (playlistGameDirectory != null && playlistGameDirectory.StartsWith('/'))
{
    playlistGameDirectory = "F:" + playlistGameDirectory; //playlistPath?.Substring(0, 2) + playlistGameDirectory;
}

List<string> gameDirFileNames = new List<string>();
if (Directory.Exists(playlistGameDirectory))
{
    gameDirFileNames = Directory.GetFiles(playlistGameDirectory).Select(f => Path.GetFileName(f)).ToList();
}

//Populate list of files in playlist.
var playlistFileNames = new List<string>();

var playlistEntries = jObject["items"]?.AsArray() ?? new JsonArray();
foreach (var entry in playlistEntries)
{
    var path = entry!["path"]?.ToString();
    
    if (!string.IsNullOrEmpty(path))
    {
        playlistFileNames.Add(Path.GetFileName(path));
    }
}

//Compare the two lists.
if (gameDirFileNames.Count > 0 && playlistFileNames.Count > 0)
{
    var invalidFileNames = gameDirFileNames.Except(playlistFileNames).ToList();

    if (invalidFileNames.Count > 0)
    {
        Console.WriteLine($"\nFound {invalidFileNames.Count} invalid files in {playlistPath}:");

        foreach(var file in invalidFileNames)
        {
            Console.WriteLine(file);
        }
        
        Console.Write("\nWould you like to save this list? (y/N) >");
        var input = Console.ReadLine() ?? string.Empty;

        if (input.ToLowerInvariant() == "y")
        {
            File.WriteAllLines(Path.GetDirectoryName(playlistPath) + "invalidFiles.txt", invalidFileNames);
            Console.WriteLine($"Saved list of invalid filenames to {Path.GetDirectoryName(playlistPath) + "invalidFiles.txt"}");
        }

        Console.Write($"\nWould you like to clean the directory {playlistGameDirectory} of the invalid files? (y/N) >");
        input = Console.ReadLine() ?? string.Empty;

        if (input.ToLowerInvariant() == "y")
        {
            var deleteCount = 0;
            foreach (var file in invalidFileNames)
            {
                var filePath = Path.Combine(playlistGameDirectory, file);
                try
                {  
                    if (File.Exists(filePath))
                    { 
                        File.Delete(filePath);
                    }
                    deleteCount++;
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Error deleting {filePath}: {ioEx.Message}");
                }
            }

            Console.WriteLine($"Deleted {deleteCount} files.");
        }
    }
}
else
{
    Console.WriteLine("\nCould not parse files. Please check the playlist configuration.");
}

Console.WriteLine("\n\nPress any key to terminate...");
Console.ReadKey();