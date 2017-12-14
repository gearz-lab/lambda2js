using System;
using System.IO;
using System.Xml;

namespace ProjectsGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Environment.CurrentDirectory = {Environment.CurrentDirectory}");

            // TEST PROJECTS:
            //
            // Test projects are generated from the main multi-targeted test project:
            // "Lambda2Js.Tests.csproj" file.

            {

                var l2jsTests = new XmlDocument();
                l2jsTests.Load("../Lambda2Js.Tests/Lambda2Js.Tests.csproj");

                var targetsElement = l2jsTests.SelectNodes("//TargetFrameworks");
                Console.WriteLine(targetsElement[0].InnerText);
                var targets = targetsElement[0].InnerText.Split(";");
                foreach (var target in targets)
                {
                    if (File.Exists($"../Lambda2Js.Tests/Lambda2Js.Tests.{target}.csproj"))
                        continue;

                    var l2jsTestNew = (XmlDocument)l2jsTests.Clone();
                    var node = l2jsTestNew.SelectSingleNode("//TargetFrameworks");
                    var targetElement = l2jsTestNew.CreateElement("TargetFramework");
                    targetElement.InnerText = target;
                    node.ParentNode.ReplaceChild(targetElement, node);
                    l2jsTestNew.Save($"../Lambda2Js.Tests/Lambda2Js.Tests.{target}.csproj");
                }

            }

            // SIGNED ASSEMBLY
            //
            // Signed assembly is generated from the main "Lambda2Js.csproj" file.

            {

                var l2js = new XmlDocument();
                l2js.Load("../Lambda2Js/Lambda2Js.csproj");

                var sig = (XmlDocument)l2js.Clone();
                var sigMain = sig.SelectSingleNode("//TargetFrameworks").ParentNode;
                var ver = sigMain.SelectSingleNode("Version").InnerText;

                sigMain.SelectSingleNode("AssemblyVersion").InnerText = $"{ver}.0";
                sigMain.SelectSingleNode("FileVersion").InnerText = $"{ver}.0";
                sigMain.SelectSingleNode("PackageId").InnerText = "Lambda2Js.Signed";
                sigMain.AppendChild(sig.CreateElement("SignAssembly").With(x => x.InnerText = "true"));
                sigMain.AppendChild(sig.CreateElement("AssemblyOriginatorKeyFile").With(x => x.InnerText = @"..\Lambda2Js.snk"));
                sigMain.AppendChild(sig.CreateElement("RootNamespace").With(x => x.InnerText = "Lambda2Js"));
                sigMain.AppendChild(sig.CreateElement("AssemblyName").With(x => x.InnerText = "Lambda2Js.Signed"));

                foreach (XmlNode doc in sig.SelectNodes("DocumentationFile"))
                    doc.InnerText = doc.InnerText.Replace("Lambda2Js.xml", "Lambda2Js.Signed.xml");

                sig.Save($"../Lambda2Js/Lambda2Js.Signed.csproj");

            }

            Console.WriteLine("Hello World!");
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
