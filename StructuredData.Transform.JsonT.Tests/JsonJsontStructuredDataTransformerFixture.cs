using System.CodeDom;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace StructuredData.Transform.JsonT.Tests
{
    [TestFixture]
    public class JsonJsontStructuredDataTransformerFixture
    {
        private string LoadEmbeddedResource(string file)
        {
            var array = Assembly.GetAssembly(typeof(JsonJsontStructuredDataTransformerFixture)).GetManifestResourceNames();
            var path = array.FirstOrDefault(s => s.EndsWith("." + file));
            if (path != null)
            {
                var stream = Assembly.GetAssembly(typeof(JsonJsontStructuredDataTransformerFixture)).GetManifestResourceStream(path);
                return stream == null ? null : new StreamReader(stream).ReadToEnd();
            }
            return null;
        }

        [Test]
        [TestCase("singleObject.json", "singleObject.jsont", "SingleObject.result")]
        [TestCase("ObjectChild.json", "ObjectChild.jsont", "ObjectChild.result")]
        [TestCase("ChildArray.json", "ChildArray.jsont", "ChildArray.result")]
        public void TestJsontTransform(string jsonFile, string transformFile, string resultFile)
        {
            var json = LoadEmbeddedResource(jsonFile);
            var transform = LoadEmbeddedResource(transformFile);
            var result = LoadEmbeddedResource(resultFile);
            var candidate = json.Transform(transform, "application/json", "jsont");
            Assert.That(candidate, Is.EqualTo(result));
        }
    }
}