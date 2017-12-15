using System;
using System.IO;
using System.Xml;

namespace ProjectsGenerator
{
    class Program
    {
        private static readonly string ProjectName = "Lambda2Js";

        static void Main(string[] args)
        {
            Console.WriteLine($"Environment.CurrentDirectory = {Environment.CurrentDirectory}");

            // TEST PROJECTS:
            //
            // Test projects are generated from the main multi-targeted test project:
            // "{ProjectName}.Tests.csproj" file.

            {

                var l2jsTests = new XmlDocument();
                l2jsTests.Load($"../{ProjectName}.Tests/{ProjectName}.Tests.csproj");

                var targetsElement = l2jsTests.SelectNodes("//TargetFrameworks");
                Console.WriteLine(targetsElement[0].InnerText);
                var targets = targetsElement[0].InnerText.Split(";");
                foreach (var target in targets)
                {
                    if (File.Exists($"../{ProjectName}.Tests/{ProjectName}.Tests.{target}.csproj"))
                        continue;

                    var l2jsTestNew = (XmlDocument)l2jsTests.Clone();
                    var node = l2jsTestNew.SelectSingleNode("//TargetFrameworks");
                    var targetElement = l2jsTestNew.CreateElement("TargetFramework");
                    targetElement.InnerText = target;
                    node.ParentNode.ReplaceChild(targetElement, node);
                    l2jsTestNew.Save($"../{ProjectName}.Tests/{ProjectName}.Tests.{target}.csproj");
                }

            }

            // SIGNED ASSEMBLY
            //
            // Signed assembly is generated from the main "{ProjectName}.csproj" file.

            {

                var l2js = new XmlDocument();
                l2js.Load($"../{ProjectName}/{ProjectName}.csproj");

                var sig = (XmlDocument)l2js.Clone();
                var sigMain = sig.SelectSingleNode("//TargetFrameworks").ParentNode;
                var ver = sigMain.SelectSingleNode("Version").InnerText;

                sigMain.SelectSingleNode("AssemblyVersion").InnerText = $"{ver}.0";
                sigMain.SelectSingleNode("FileVersion").InnerText = $"{ver}.0";
                sigMain.SelectSingleNode("PackageId").InnerText += ".Signed";
                sigMain.AppendChild(sig.CreateElement("SignAssembly").With(x => x.InnerText = "true"));
                sigMain.AppendChild(sig.CreateElement("AssemblyOriginatorKeyFile").With(x => x.InnerText = $@"..\{ProjectName}.snk"));

                if (sigMain.SelectSingleNode("RootNamespace") == null)
                    sigMain.AppendChild(sig.CreateElement("RootNamespace").With(x => x.InnerText = $"{ProjectName}"));

                if (sigMain.SelectSingleNode("AssemblyName") == null)
                    sigMain.AppendChild(sig.CreateElement("AssemblyName").With(x => x.InnerText = $"{ProjectName}.Signed"));
                else
                    sigMain.SelectSingleNode("AssemblyName").InnerText += ".Signed";

                foreach (XmlNode doc in sig.SelectNodes("//DocumentationFile"))
                    doc.InnerText = doc.InnerText.Replace($"{ProjectName}.xml", $"{ProjectName}.Signed.xml");

                sig.Save($"../{ProjectName}/{ProjectName}.Signed.csproj");

            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }

    static class ObjectExtensions
    {
        public static T With<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }
    }
}
