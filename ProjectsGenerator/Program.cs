using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ProjectsGenerator
{
    class Program
    {
        private static readonly string ProjectName = "Lambda2Js";

        static void Main(string[] args)
        {
            // TEST PROJECTS:
            //
            // Test projects are generated from the main multi-targeted test project:
            // "{ProjectName}.Tests.csproj" file.

            var testGroups = new ListTestGroups {
                { "Tests" },
                { "NuGet.Tests" },
                { "NuGet.Signed.Tests" },
            };

            foreach (var testGroup in testGroups)
            {
                var groupName = testGroup.Name;

                var testsProjXml = new XmlDocument();
                var pathProj = $"../{ProjectName}.Tests/{ProjectName}.{groupName}.csproj";

                FindAndSetCurrentPath(pathProj);
                Console.WriteLine($"Environment.CurrentDirectory = {Environment.CurrentDirectory}");

                testsProjXml.Load(pathProj);

                var targetsElement = testsProjXml.SelectNodes("//TargetFrameworks");
                Console.WriteLine(targetsElement[0].InnerText);
                var targets = targetsElement[0].InnerText.Split(";");
                foreach (var target in targets)
                {
                    if (File.Exists($"../{ProjectName}.Tests/{ProjectName}.{groupName}.{target}.csproj"))
                        continue;

                    var testProjXml_2 = (XmlDocument)testsProjXml.Clone();
                    var node = testProjXml_2.SelectSingleNode("//TargetFrameworks");
                    var targetElement = testProjXml_2.CreateElement("TargetFramework");
                    targetElement.InnerText = target;
                    node.ParentNode.ReplaceChild(targetElement, node);
                    testProjXml_2.Save($"../{ProjectName}.Tests/{ProjectName}.{groupName}.{target}.csproj");
                }

            }

            // NUGET TEST PROJECTS:
            //
            // Test projects deployment:
            // "{ProjectName}.NuGet.Tests.csproj" file.

            {

                var testsProjXml = new XmlDocument();
                var pathProj = $"../{ProjectName}.Tests/{ProjectName}.NuGet.Tests.csproj";

                FindAndSetCurrentPath(pathProj);
                Console.WriteLine($"Environment.CurrentDirectory = {Environment.CurrentDirectory}");

                testsProjXml.Load(pathProj);

                var targetsElement = testsProjXml.SelectNodes("//TargetFrameworks");
                Console.WriteLine(targetsElement[0].InnerText);
                var targets = targetsElement[0].InnerText.Split(";");
                foreach (var target in targets)
                {
                    if (File.Exists($"../{ProjectName}.Tests/{ProjectName}.NuGet.Tests.{target}.csproj"))
                        continue;

                    var testProjXml_2 = (XmlDocument)testsProjXml.Clone();
                    var node = testProjXml_2.SelectSingleNode("//TargetFrameworks");
                    var targetElement = testProjXml_2.CreateElement("TargetFramework");
                    targetElement.InnerText = target;
                    node.ParentNode.ReplaceChild(targetElement, node);
                    testProjXml_2.Save($"../{ProjectName}.Tests/{ProjectName}.NuGet.Tests.{target}.csproj");
                }

            }

            // SIGNED ASSEMBLY
            //
            // Signed assembly is generated from the main "{ProjectName}.csproj" file.

            {

                var projXml = new XmlDocument();
                projXml.Load($"../{ProjectName}/{ProjectName}.csproj");

                var sig = (XmlDocument)projXml.Clone();
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

        private static void FindAndSetCurrentPath(string pathProj)
        {
            while (true)
            {
                try
                {
                    if (File.Exists(pathProj))
                        break;
                }
                catch (Exception)
                {
                }

                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
            }
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

    class TestGroup
    {
        public string Name { get; set; }
    }

    class ListTestGroups : List<TestGroup>
    {
        public void Add(string name)
        {
            this.Add(new TestGroup
            {
                Name = name,
            });
        }
    }
}