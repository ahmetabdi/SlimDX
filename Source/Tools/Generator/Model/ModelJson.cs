﻿// Copyright (c) 2007-2011 SlimDX Group
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Json;
using System.IO;

namespace SlimDX.Generator
{
	static class ModelJson
	{
		public static ApiModel Parse(JsonObject root)
		{
			var result = new ApiModel();
			foreach (var type in ParseTypes(root))
				result.AddType(type.Value);
			return result;
		}

		static Dictionary<string, TypeModel> ParseTypes(JsonObject root)
		{
			var types = new Dictionary<string, TypeModel>();

			JsonObject items;
			if (root.TryGetValue("dependencies", out items))
			{
				foreach (var item in items)
				{
					var dependency = JsonObject.FromJson(File.ReadAllText((string)item));
					foreach (var type in ParseTypes(dependency))
						types[type.Key] = type.Value;
				}
			}

			if (root.TryGetValue("references", out items))
			{
				foreach (var item in items)
				{
					var name = (string)item["name"];
					var type = Type.GetType((string)item["target"]);
					var model = new ReferenceModel(name, type);
					types[model.Key] = model;
				}
			}

			if (root.TryGetValue("interfaces", out items))
			{
				foreach (var item in items)
				{
					var name = (string)item["name"];
					var type = types[(string)item["type"]];
					var guid = new Guid((string)item["guid"]);
					var model = new InterfaceModel(name, guid, type);
					types[model.Key] = model;

					foreach (var method in ParseMethods(item, types))
						model.AddMethod(method);
				}
			}

			return types;
		}

		static IEnumerable<MethodModel> ParseMethods(JsonObject root, Dictionary<string, TypeModel> types)
		{
			var results = new List<MethodModel>();

			JsonObject methods;
			if (root.TryGetValue("methods", out methods))
			{
				foreach (var method in methods)
				{
					var name = (string)method["name"];
					var type = types[(string)method["type"]];
					var model = new MethodModel(name, type);
					foreach (var parameter in ParseParameters(method, types))
						model.AddParameter(parameter);

					results.Add(model);
				}
			}

			return results;
		}

		static IEnumerable<ParameterModel> ParseParameters(JsonObject root, Dictionary<string, TypeModel> types)
		{
			var results = new List<ParameterModel>();

			JsonObject parameters;
			if (root.TryGetValue("parameters", out parameters))
			{
				foreach (var parameter in parameters)
				{
					var name = (string)parameter["name"];
					var type = types[(string)parameter["type"]];
					var flags = ParseParameterFlags(parameter);
					var model = new ParameterModel(name, type, flags);

					results.Add(model);
				}
			}

			return results;
		}

		static ParameterModelFlags ParseParameterFlags(JsonObject root)
		{
			var results = ParameterModelFlags.None;

			JsonObject flags;
			if (root.TryGetValue("flags", out flags))
			{
				foreach (var flag in flags)
				{
					switch ((string)flag)
					{
						case "out":
							results |= ParameterModelFlags.IsOutput;
							break;
					}
				}
			}

			return results;
		}
	}
}