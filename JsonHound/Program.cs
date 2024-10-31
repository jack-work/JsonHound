using System.Text.Json;
using System.Text.RegularExpressions;

if (args.SingleOrDefault() is "--help")
    return WriteAndExit(
        "Your json is escaped, perhaps at multiple levels (e.g. \"{\\\"test\\\": \\\"{...}\\\"}\"); and " +
        "you want to track it down and catch it and bring it back home to where it belongs (or " +
        "in other words remove all the escaped characters and render a valid json object).  " +
        "This program can do it for you.\n\n" +
        "Arguments are defined by consecutive name value pairs.  They are not position specific.\n" +
        "- filePath: Description: file from which json may be read.  If not provided, application will " +
        "prompt for json input from console.  e.g. filePath C:\\test\n" +
        "- format: Description: comma separated list defining the format of the output.  Currently" +
        "only indent is supported.  Default System.Text.Json format used if none specified. " +
        "e.g. format indent.\n" +
        "- outFile: Description: file to which the output will be written.  If none is specified " +
        "then the output will be written to console. e.g. outFile C:\\pcf.json.\n\n" +
        "Program supports absolute or relative file paths.  Arguments can come in any order.\n\n" +
        "Full argument example: " +
        "outFile C:\\escapedJson.json format indent outFile pcf.json");
var (filePath, format, outFile) = ParseArgs(args);
string? jsonText;
if (!TryReadFile(filePath, out jsonText) && !TryReadConsole("Input escaped json: ", out jsonText))
    return WriteAndExit("Input text is null, which is valid json.");

if (!TryRecoverAndDeserializeSingleLevel(jsonText, out var root))
    return WriteAndExit(JsonSerializer.Serialize(root, GetJsonSerializerOptions()));

var recoveredJson = RecoverJson(root?.RootElement);
var serializedJson = JsonSerializer.Serialize(recoveredJson, GetJsonSerializerOptions());
if (outFile is not null)
{
    File.WriteAllText(outFile, serializedJson);
    Console.WriteLine($"Json content written to {outFile}");
    return 0;
}

return WriteAndExit(serializedJson);

JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions()
{
    WriteIndented = format?.Split(',')?.Any(item => item.ToLower().Equals("indent")) ?? false
};

bool TryReadConsole(string message, out string? jsonText)
{
    Console.WriteLine(message);
    jsonText = Console.ReadLine();
    return jsonText is not null;
}

(string? filePath, string? format, string? outFile) ParseArgs(string[] arguments) => 
    TryParseArgsToDictionary(arguments, out var dict) ? (
        filePath: dict.TryGetValue("filePath", out var filePath) ? filePath : null,
        format: dict.TryGetValue("format", out var format) ? format : null,
        outFile: dict.TryGetValue("outFile", out var outFile) ? outFile : null
    ) : (null, null, null);

bool TryReadFile(string? filePath, out string? fileText) => File.Exists(filePath) ?
    SetReturn(set: out fileText, to: File.ReadAllText(filePath), @return: true) :
    SetReturn(set: out fileText, to: null, @return: false);

object? RecoverJson(JsonElement? elem)
{
    if (!elem.HasValue) return null;
    return elem.Value.ValueKind switch
    {
        JsonValueKind.Undefined => null,
        JsonValueKind.Object => elem.Value.EnumerateObject().ToDictionary(property => property.Name, property => RecoverJson(property.Value)),
        JsonValueKind.Array => elem.Value.EnumerateArray().Select(element => RecoverJson(element)).ToList(),
        JsonValueKind.String => TryRecoverAndDeserializeSingleLevel(elem.Value.GetString(), out var doc) ? RecoverJson(doc?.RootElement) : doc?.RootElement,
        JsonValueKind.Number => elem,
        JsonValueKind.True => elem,
        JsonValueKind.False => elem,
        JsonValueKind.Null => elem,
        _ => throw new ArgumentOutOfRangeException()
    };
}

int WriteAndExit(string message)
{
    Console.WriteLine(message);
    return 0;
}

// Returns true if and only if the provided string encodes an aggregate json type instance,
// array or object, implying that more deserialization may be necessary
bool TryRecoverAndDeserializeSingleLevel(string? s, out JsonDocument? doc)
{
    if (s is null) return SetReturn(set: out doc, to: null, @return: false);

    // If the string begins with a " then we suppose that it must be de-escaped.
    var fixedS = s is not ['"', ..] ? s : new M<string>(s)
        .Bind(str => str.Trim('"'))
        .Bind(str => Regex.Replace(str, @"\\""", @""""))
        .Bind(str => Regex.Replace(str, @"\\\\", @"\"))
        .Value;

    // If the string does not encode an aggregate type then we reencode it in jsondocument
    // and return it for type uniformity.
    if (fixedS is not ['[', ..] && fixedS is not ['{', ..])
        return SetReturn(set: out doc, to: JsonDocument.Parse($"\"{s}\""), @return: false);
 
    return SetReturn(set: out doc, to: JsonSerializer.Deserialize<JsonDocument>(fixedS), @return: true);
}

// Dumb function that sets a variable and returns the provided value so that the two ops
// can be written in one statement.
T2 SetReturn<T1, T2>(out T1 set, T1 to, T2 @return)
{
    set = to;
    return @return;
}

bool TryParseArgsToDictionary(string[] strings, out Dictionary<string, string> dict)
{
    dict = strings
        .Select((value, index) => new { Value = value, Index = index })
        .Where(x => x.Index % 2 == 0)
        .Select(x => (key: x.Value, val: strings[x.Index + 1]))
        .ToDictionary(x => x.key, x => x.val);
    return dict.Any();
}

class M<T>(T s)
{
    public M<U> Bind<U>(Func<T, U> bind) => new M<U>(bind(s));
    public M<U> Bind<U>(Func<T, M<U>> bind) => bind(s);
    public T Value => s;
}
