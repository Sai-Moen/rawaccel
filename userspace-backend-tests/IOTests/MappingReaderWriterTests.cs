using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data;
using userspace_backend.IO;

namespace userspace_backend_tests.IOTests
{
    [TestClass]
    public class MappingReaderWriterTests
    {
        public static string TestDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"TestFiles\MappingReaderWriter");
        public static string ExpectedOutputs = Path.Combine(TestDirectory, "ExpectedOutputs");
        public static string TestInputs = Path.Combine(TestDirectory, "Inputs");

        [TestMethod]
        public void GivenValidInput_Writes()
        {
            var mapping = new Mapping()
            {
                ProfilesToGroups = new Dictionary<string, MappingGroups>()
                {
                    { "Default", new MappingGroups() { Groups = new List<string>() { "Default", "Logitech Mice", } } },
                    { "Test", new MappingGroups() { Groups = new List<string>() { "Test Mice", } } },
                },
            };

            var writer = new MappingReaderWriter();
            var writePath = Path.Combine(TestDirectory, "testMapping.json");
            writer.Write(writePath, mapping);

            Assert.IsTrue(File.Exists(writePath));
            string actualOutput = File.ReadAllText(writePath);

            string expectedWriteOutputFile = Path.Combine(ExpectedOutputs, "expectedWriteOutput.json");
            string expectedOutput = File.ReadAllText(expectedWriteOutputFile);
            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
