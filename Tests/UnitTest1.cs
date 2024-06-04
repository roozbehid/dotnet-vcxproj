using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        string nuget = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key = ""MyLocal"" value=""C:\Users\Win10Home\Documents\(inContact)"" />
  </packageSources>
</configuration>
";
        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
