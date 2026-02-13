using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

if (args.Length <= 0)
{
    Console.WriteLine("Usage: DNGuardDecoder.exe /path/to/assembly.dll");
    return;
}

var selectedFile = args[0];

if(!File.Exists(selectedFile))
{
    Console.WriteLine($"File '${selectedFile}' doesn't exist.");
    return;
}

var module = ModuleDefMD.Load(selectedFile);

foreach (var type in module.Types)
{
    if (!type.HasMethods)
        continue;
    
    foreach (var method in type.Methods)
    {
        if (method.HasBody && method.Body.HasInstructions)
        {
            var IL = method.Body.Instructions;
            for (int i = 0; i < IL.Count; i++)
            {
                if (IL[i].OpCode == OpCodes.Call && IL[i].Operand == null && IL[i + 3].OpCode == OpCodes.Br_S)
                {
                    Console.WriteLine($"Detected InvalidMD @ {method.Name} ({i})");
                    IL[i].OpCode = OpCodes.Nop;
                    IL[i + 3].OpCode = OpCodes.Nop;
                }
            }
        }
    }
}

var ee = new ModuleWriterOptions(module)
{
    MetadataLogger = DummyLogger.NoThrowInstance
};

var directory = Path.GetDirectoryName(selectedFile) ?? Environment.CurrentDirectory;
var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFile);
var extension = Path.GetExtension(selectedFile);
var outputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_Decoded{extension}");

Console.WriteLine($"Output: {outputPath}");
module.Write(outputPath, ee);
