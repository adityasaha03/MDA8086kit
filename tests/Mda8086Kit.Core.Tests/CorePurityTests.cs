using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Mda8086Kit.Core.Tests
{
    public class CorePurityTests
    {
        [Fact]
        public void Method_CoreAssembly_ReferencesNoForbiddenAssemblies()
        {
            // Core assembly
            Assembly coreAssembly = typeof(Mda8086Kit.Core.Dummy).Assembly;
            var refAssemblies = coreAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

            Assert.DoesNotContain("System.Windows.Forms", refAssemblies);
            Assert.DoesNotContain("System.Drawing", refAssemblies);
            Assert.DoesNotContain("System.Drawing.Common", refAssemblies);
        }

        [Fact]
        public void Method_CoreSource_ContainsNoForbiddenTokens()
        {
            string[] forbiddenTokens = new[]
            {
                "System.Windows.Forms",
                "System.Drawing",
                "DllImport",
                "DateTime.Now",
                "Thread.Sleep",
                "FileStream",
                "File.",
                "Application.DoEvents"
            };

            // Assuming tests run in bin\Debug\net8.0, walk up to src/Mda8086Kit.Core
            string baseDir = AppContext.BaseDirectory;
            string srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "Mda8086Kit.Core"));

            Assert.True(Directory.Exists(srcDir), $"Source directory not found: {srcDir}");

            var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in csFiles)
            {
                if (file.Contains("obj") || file.Contains("bin")) continue;
                
                string content = File.ReadAllText(file);
                foreach (var token in forbiddenTokens)
                {
                    Assert.DoesNotContain(token, content);
                }
            }
        }
    }
}
