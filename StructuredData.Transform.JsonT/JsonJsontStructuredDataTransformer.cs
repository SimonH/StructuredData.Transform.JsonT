using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using IronPython.Hosting;
using IronPython.Modules;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PythonLibs4CSharp;
using StructuredData.Transform.interfaces;

namespace StructuredData.Transform.JsonT
{
    [Export(typeof(ITransformStructuredData))]
    [ExportMetadata("MimeType", "application/json")]
    [ExportMetadata("Method", "jsont")]
    public class JsonJsontStructuredDataTransformer : ITransformStructuredData
    {
        private string LoadEmbeddedResource(string file)
        {
            var array = Assembly.GetAssembly(typeof(JsonJsontStructuredDataTransformer)).GetManifestResourceNames();
            var path = array.FirstOrDefault(s => s.EndsWith("." + file));
            if (path != null)
            {
                var stream = Assembly.GetAssembly(typeof(JsonJsontStructuredDataTransformer)).GetManifestResourceStream(path);
                return stream == null ? null : new StreamReader(stream).ReadToEnd();
            }
            return null;
        }

        private PythonDictionary CreatePythonDictionary(IDictionary<string, object> dictionary)
        {
            var pD = new PythonDictionary();
            foreach (var item in dictionary)
            {
                var subDict = item.Value as IDictionary<string, object>;
                var subList = item.Value as IList<object>;
                if (subDict != null)
                {
                    pD.Add(item.Key, CreatePythonDictionary(subDict));
                }
                else if (subList != null)
                {
                    var list = new List();
                    foreach (var listItem in subList)
                    {
                        if (listItem is IDictionary<string, object>)
                        {
                            list.Add(CreatePythonDictionary((IDictionary<string, object>) listItem));
                        }
                        else
                        {
                            list.Add(listItem);
                        }
                    }
                    pD.Add(item.Key, list);
                }
                else
                {
                    pD.Add(item.Key, item.Value);
                }
            }
            return pD;
        }

        public string Transform(string sourceData, string transformData)
        {
            var engine = Python.CreateEngine();
            StdLib.Import(engine);
            var scope = engine.CreateScope();
            var scriptSource = engine.CreateScriptSourceFromString(LoadEmbeddedResource("jsontemplate.py"), SourceCodeKind.Statements);
            var compiled = scriptSource.Compile();
            compiled.Execute(scope);
            var expand = scope.GetVariable("expand");
            var converter = new ExpandoObjectConverter();
            dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(sourceData, converter);
            var pythonDict = CreatePythonDictionary((IDictionary<string, object>)jsonObject);
            return engine.Operations.Invoke(expand, new object[] { transformData, pythonDict }).ToString();
        }
    }
}