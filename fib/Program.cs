using System.CommandLine;
using System.Runtime.InteropServices;

var languageExtensions = new Dictionary<string, string[]>
        {
            { "c#", new[] { "cs" } },
            { "javascript", new[] { "js" } },
            { "python", new[] { "py" } },
            { "java", new[] { "java" } },
            { "html", new[] { "html", "htm" } },
            { "css", new[] { "css" } },
            {"c++",new[]{ "h","cpp"} }
        };
   var outputOption = new Option<FileInfo>("--output","file path and name")
   {
    IsRequired = true
   };
outputOption.AddAlias("-o");
var languagesOption = new Option<string>("--languages", description:
    "Comma-separated list of programming languages" +
    "(e.g. C#, JavaScript, Python)" + ". Use 'all' to include all files.")
{
    IsRequired = true
};
languagesOption.AddAlias("-l");
var noteOption = new Option<bool>("--note", " write the source code as a comment in the bundle file.");
noteOption.AddAlias("-n");
var sortOption = new Option<string>("--sort", getDefaultValue: () => "name",
description: "Sort files by 'name' or 'type")
{
    IsRequired = false
};
sortOption.AddAlias("-s");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "remove empty line or not");
removeEmptyLinesOption.AddAlias("-r");
var authorOption = new Option<string>("--author","write the namee of authors");
authorOption.AddAlias("-a");
var bundleCommand = new Command("bundle", "bundle files code to a single file");
var createRspCommand = new Command("create-rsp", "Create a response file for the create command");
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languagesOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);
bundleCommand.SetHandler((FileInfo output, string languages,bool note,string sort,bool empty,string author) =>
{
    if (string.IsNullOrEmpty(sort))
        Console.WriteLine("you must write name or type");
    else { Console.WriteLine($"Sort option received: {sort}"); }
   string[] languages1 = languages.Split(',');
    try
    {
        if (string.IsNullOrEmpty(sort))
        {
            sort = "name"; // ברירת מחדל
        }

        var directoryPath = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        files = files.Where(file => !file.Contains(Path.Combine("bin", "")) &&
                             !file.Contains(Path.Combine("debug", "")) &&
                             !file.Contains(Path.Combine("obj", ""))) // הוספת סינון לתיקיות bin, debug, obj
             .ToArray();

        // מיון הקבצים
        files = sort.ToLower() == "type"
            ? files.OrderBy(path => Path.GetExtension(path)).ToArray()
            : files.OrderBy(path => Path.GetFileName(path)).ToArray();

        // סינון קבצים לפי שפות אם יש צורך
        if (languages1[0].ToLower() != "all")
        {
            var invalidLanguages = languages1.Where(lang => !languageExtensions.ContainsKey(lang.ToLower())).ToArray();
            if (invalidLanguages.Any())
            {
                Console.WriteLine("The following languages are not recognized: " + string.Join(", ", invalidLanguages));
            }

            var filterEndLanguages = languages1
                .SelectMany(lang => languageExtensions.ContainsKey(lang.ToLower())
                    ? languageExtensions[lang.ToLower()]
                    : Enumerable.Empty<string>())
                .ToArray();

            files = files.Where(file => filterEndLanguages.Any(ext => file.EndsWith($".{ext}", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (!files.Any())
            {
                Console.WriteLine("No files found matching the specified languages.");
                return;
            }
        }

        // סינון קובץ הפלט מתוך הרשימה של הקבצים
        files = files.Where(file => !file.Equals(output.FullName, StringComparison.OrdinalIgnoreCase)).ToArray();

        // כתיבה לקובץ
        using (var writer = new StreamWriter(output.FullName))
        {
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"####{author}####");
            }
            foreach (var file in files)
            {
                string content = File.ReadAllText(file);
                if (empty) // אם צריך להימנע משורות ריקות
                {
                    // הסרת שורות ריקות מהתוכן 
                    content = string.Join(Environment.NewLine, content
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                    File.WriteAllText(file, content);
                }                
                if (note)
                {
                   writer.WriteLine($"###{file}###"); // הערה עם שם קובץ
                }  

                writer.WriteLine(content);
            }
        }
        Console.WriteLine($"Files successfully bundled into {output.FullName}");
    }
    catch (DirectoryNotFoundException exe)
    {
       Console.WriteLine("Error: path file is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, outputOption, languagesOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);
    createRspCommand.SetHandler(() =>
    {       
            // השאלות שהמשתמש יתבקש להזין
            Console.WriteLine("Please enter the following details for the 'create-rsp' command:");

            // קלט למשתנים
            Console.Write("Enter the output file name for example output.txt):or path to file ");
            string output = Console.ReadLine();

            Console.Write("Enter the languages (comma separated, for example. c#,javascript or all for alllanguages ");
            string languages = Console.ReadLine();
            // ולידציה עבור languages
            if (string.IsNullOrWhiteSpace(languages))
            {
                Console.WriteLine("Error: You must provide at least one language.");
                return;
            }
            // ולידציה עבור sort
            Console.Write("you want sort? (true/false)");
            bool isSort = bool.Parse(Console.ReadLine());
            string sort = "";            
            if (isSort)
            {
                 Console.Write("Sort by (name/type): ");
                 sort = Console.ReadLine();
                if (string.IsNullOrEmpty(sort))
                {
                    Console.WriteLine("Error: Sort option must be filled");
                }
            }
            Console.Write("Remove empty lines (true/false):");
            bool removeEmptyLines;
            if (!bool.TryParse(Console.ReadLine(), out removeEmptyLines))
            {
                Console.WriteLine("Error: Invalid value for remove-empty-lines. Please enter true or false.");
                return;
            }

            Console.Write("Enter author name (optional):");
            string author = Console.ReadLine();

            // יצירת פקודה מלאה
            string fullCommand = "bundle ";
            if (!string.IsNullOrEmpty(output))
                fullCommand += $"\n--output {output} ";
            else
            {
                fullCommand += "--output bundle.txt";
            }

            if (!string.IsNullOrEmpty(languages))
                fullCommand += $"\n--languages {languages} ";

            fullCommand += !string.IsNullOrEmpty(sort) ? $"\n--sort {sort} " : "";
            fullCommand += removeEmptyLines ? "\n--remove-empty-lines " : "";
            if (!string.IsNullOrEmpty(author))
                fullCommand += $"\n--author {author} ";

            // שמירת הפקודה בקובץ תגובה
            string fileName = "responseFile.rsp";
            File.WriteAllText(fileName, fullCommand.Trim());

            Console.WriteLine($"Response file '{fileName}' created successfully!");
        Console.WriteLine("for run run dotnet @responseFile.rsp");
        });
        var rootCommand = new RootCommand("root command for bundler files in cli");
        rootCommand.AddCommand(bundleCommand);
        rootCommand.AddCommand(createRspCommand);    
        rootCommand.InvokeAsync(args);
   