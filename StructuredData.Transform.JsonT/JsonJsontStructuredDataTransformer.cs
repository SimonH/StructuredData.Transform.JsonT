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
                pD.Add(item.Key, CheckValue(item.Value));
            }
            return pD;
        }

        private List CreatePythonList(IList<object> list)
        {
            var pL = new List();
            foreach(var item in list)
            {
                pL.Add(CheckValue(item));
            }
            return pL;
        } 

        private object CheckValue(object value)
        {
            var objects = value as IDictionary<string, object>;
            if(objects != null)
            {
                return CreatePythonDictionary(objects);
            }

            var list = value as IList<object>;
            return list != null ? CreatePythonList(list) : value;
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