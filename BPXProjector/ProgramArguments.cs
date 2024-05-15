using System.Xml;
using CommandLine;

namespace BPXProjector;

[AttributeUsage(AttributeTargets.Field)]
internal class InformationalAttribute(string helpText) : Attribute {
    internal string HelpText = helpText;
}

internal static class XMLExtensions {
    internal static XmlElement AddNewElement(this XmlElement element, string elementName, string label = "", int tabLevel = 0)
    {
        if (tabLevel > 0)
            element.AppendChild(element.OwnerDocument.CreateWhitespace(new string('\t', tabLevel)));
        var newElem = element.OwnerDocument.CreateElement(elementName);
        if (label != string.Empty)
            newElem.SetAttribute("Label", label);
        element.AppendChild(newElem);
        element.AppendChild(element.OwnerDocument.CreateWhitespace("\n"));
        return newElem;
    }
}

[Verb("install", HelpText = "Create new things")]
public class InstallArgs {
    
    public enum Command {
        [Informational("Lists available options")] _,
        [Informational("Installs CommonLibs Project file.")] CommonLibs,
        [Informational("Installs Shoddy GameLibs Project file.")] GameLibs
    }

    [Value(0, Default = Command._, HelpText = "Which thing to install.")]
    public Command SubCommand { get; set; }

    [Option('D', "dest", Default = "./", HelpText = "The location to drop the new item.")]
    public string Destination { get; set; } = null!;
    
    [Option('S', "soft", FlagCounter = true, HelpText = "Don't do intensive work, like editing CSPROJ files and so on.")]
    public int Soft { get; set; }
    
    [Option('F', "force", FlagCounter = true, HelpText = "Force overwrites.")]
    public int Force { get; set; }

    public int Success()
    {
        if (!Directory.Exists(Destination))
            Directory.CreateDirectory(Destination!);
        switch (SubCommand)
        {
            case Command._:
                foreach (var fieldInfo in typeof(Command).GetFields().Where(fieldInfo => !fieldInfo.Name.Contains('_')))
                {
                    Console.WriteLine($"\t{fieldInfo.Name} - {fieldInfo.CustomAttributes
                        .First(attr => attr.AttributeType == typeof(InformationalAttribute)).ConstructorArguments.FirstOrDefault()}");
                }
                break;
            case Command.GameLibs:
                ImportStreamTarget(ResourceManager.GameLibsTargetsKey, ResourceManager.GameLibsTargets, propGroup =>
                {
                    propGroup.AddNewElement("GameFiles", tabLevel: 2).InnerText = "$(STEAMAPPS)Game Name/";
                    propGroup.AddNewElement("BuildGameLibs", "Build ShoddyGameLibs?", 2).InnerText = "false";
                    propGroup.AddNewElement("BuildFromGameLibs", "Build From ShoddyGameLibs?",2).InnerText = "false";
                });
                break;
            case Command.CommonLibs:
                ImportStreamTarget(ResourceManager.CommonLibsTargetsKey, ResourceManager.CommonLibsTargets, propGroup =>
                {
                    propGroup.AddNewElement("UnityVersion", tabLevel: 2).InnerText = "20xx.x.x";
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return 0;
    }

    private void ImportStreamTarget(string resourceKey, Stream? resourceStream, Action<XmlElement>? customConfig = null)
    {
        var labelName = resourceKey.Split('.').First();
        var newTargetFile = Path.Combine(Destination, resourceKey);
        CreateTargetsFile(newTargetFile, resourceStream);
                
        Console.WriteLine($"Installed {labelName} @ '{Destination}'");

        if (Soft > 0) return;
                
        if (!TryGetCSProj(Destination, out var projectLocation, out var loadedCSDoc, out var projectElement)) return;

        if (AlreadyContainsLabels(projectElement!, labelName, Force > 0) && Force == 0)
        {
            Console.WriteLine("[WARN] Skipping import to CSPROJ because it already exists, to overwrite use -F");
            return;
        }
                
        StripWhitespace(projectElement);
        projectElement!.AppendChild(loadedCSDoc!.CreateWhitespace("\n\n"));

        var propGroup = projectElement.AddNewElement("PropertyGroup", labelName, 1);
        propGroup.AppendChild(loadedCSDoc.CreateWhitespace("\n"));
        customConfig?.Invoke(propGroup);
        propGroup.AppendChild(loadedCSDoc.CreateWhitespace("\t"));
        
        var import = projectElement.AddNewElement("Import", labelName, 1);
        import.SetAttribute("Project", $"./{resourceKey}");
                
        loadedCSDoc!.Save(projectLocation!);
                
        Console.WriteLine($"Imported {labelName} to CSPROJ @ '{projectLocation}'");
    }
    
    private static void StripWhitespace(XmlNode? projectElement)
    {
        while (projectElement!.LastChild is XmlWhitespace)
            projectElement.RemoveChild(projectElement.LastChild);
    }
    private static bool AlreadyContainsLabels(XmlElement projectElement, string label, bool remove = false)
    {
        List<XmlElement> toRemove = [ ];
        var containsLabels = false;
        foreach (var xmlElemChildNode in projectElement.ChildNodes)
        {
            if (xmlElemChildNode is not XmlElement xmlElemChild) continue;
            if (!xmlElemChild.HasAttribute("Label") || xmlElemChild.GetAttribute("Label") != label) continue;
            if (remove)
                toRemove.Add(xmlElemChild);
            else
                containsLabels = true;
        }
        foreach (var xmlElement in toRemove)
            projectElement.RemoveChild(xmlElement);
        return containsLabels;
    }
    private static bool TryGetCSProj(string destination, out string? csProjLocation, out XmlDocument? xmlDocument, out XmlElement? projectElement)
    {
        xmlDocument = default;
        projectElement = default;
        csProjLocation = default;
        var csProjFiles = Directory.GetFiles(destination, "*.csproj");
        if (csProjFiles.Length == 0) return false;

        csProjLocation = csProjFiles[0];
        xmlDocument = new XmlDocument {
            PreserveWhitespace = true
        };
        xmlDocument.Load(csProjLocation);
        var docChildNode = xmlDocument!.SelectSingleNode("Project");
        if (docChildNode is not XmlElement projElem) return false;
        projectElement = projElem;
        return true;
    }
    private static void CreateTargetsFile(string newTargetFile, Stream? gameLibsTargets)
    {
        var createdCommonsTargets = File.Create(newTargetFile);
        gameLibsTargets?.CopyToAsync(createdCommonsTargets);
        createdCommonsTargets.Close();
    }
}
