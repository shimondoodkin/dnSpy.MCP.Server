/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Text;
using dnSpy.MCP.Server.Contracts;
using dnSpy.MCP.Server.Application;
using dnSpy.MCP.Server.Helper;

namespace dnSpy.MCP.Server.Application
{
    [Export(typeof(McpTools))]
    public sealed class McpTools
    {
        readonly IDocumentTreeView documentTreeView;
        readonly IDecompilerService decompilerService;
        readonly Lazy<AssemblyTools> assemblyTools;
        readonly Lazy<TypeTools> typeTools;
        readonly Lazy<UsageFindingCommandTools> usageFindingCommandTools;

        [ImportingConstructor]
        public McpTools(
            IDocumentTreeView documentTreeView,
            IDecompilerService decompilerService,
            Lazy<AssemblyTools> assemblyTools,
            Lazy<TypeTools> typeTools,
            Lazy<UsageFindingCommandTools> usageFindingCommandTools)
        {
            this.documentTreeView = documentTreeView;
            this.decompilerService = decompilerService;
            this.assemblyTools = assemblyTools;
            this.typeTools = typeTools;
            this.usageFindingCommandTools = usageFindingCommandTools;
        }

        public List<ToolInfo> GetAvailableTools()
        {
            return new List<ToolInfo> {
                new ToolInfo {
                    Name = "list_assemblies",
                    Description = "List all loaded assemblies in dnSpy",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>(),
                        ["required"] = new List<string>()
                    }
                },
                new ToolInfo {
                    Name = "get_assembly_info",
                    Description = "Get detailed information about a specific assembly",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["cursor"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Optional cursor for pagination"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name" }
                    }
                },
                new ToolInfo {
                    Name = "list_types",
                    Description = "List all types in an assembly or namespace",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["namespace"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Optional namespace filter"
                            },
                            ["cursor"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Optional cursor for pagination"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name" }
                    }
                },
                new ToolInfo {
                    Name = "get_type_info",
                    Description = "Get detailed information about a specific type",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["cursor"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Optional cursor for pagination"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name" }
                    }
                },
                new ToolInfo {
                    Name = "decompile_method",
                    Description = "Decompile a specific method to C# code",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                },
                new ToolInfo {
                    Name = "search_types",
                    Description = "Search for types by name across all loaded assemblies",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["query"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Search query (supports wildcards)"
                            },
                            ["cursor"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Optional cursor for pagination"
                            }
                        },
                        ["required"] = new List<string> { "query" }
                    }
                },
                new ToolInfo {
                    Name = "list_methods_in_type",
                    Description = "List all methods in a type",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["visibility"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Filter by visibility (public, private, protected, internal)"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name" }
                    }
                },
                new ToolInfo {
                    Name = "list_properties_in_type",
                    Description = "List all properties in a type",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name" }
                    }
                },
                new ToolInfo {
                    Name = "get_method_signature",
                    Description = "Get detailed method signature",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                },
                new ToolInfo {
                    Name = "find_who_calls_method",
                    Description = "Find all methods that call a specific method",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                },
                new ToolInfo {
                    Name = "analyze_type_inheritance",
                    Description = "Analyze complete inheritance chain of a type",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name" }
                    }
                },
                new ToolInfo {
                    Name = "list_tools",
                    Description = "List all available MCP tools",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>()
                    }
                },
                new ToolInfo {
                    Name = "get_method_il",
                    Description = "Get IL instructions of a method",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                },
                new ToolInfo {
                    Name = "get_method_il_bytes",
                    Description = "Get raw IL bytes of a method",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                },
                new ToolInfo {
                    Name = "get_method_exception_handlers",
                    Description = "Get exception handlers (try-catch-finally) of a method",
                    InputSchema = new Dictionary<string, object> {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object> {
                            ["assembly_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the assembly"
                            },
                            ["type_full_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Full name of the type"
                            },
                            ["method_name"] = new Dictionary<string, object> {
                                ["type"] = "string",
                                ["description"] = "Name of the method"
                            }
                        },
                        ["required"] = new List<string> { "assembly_name", "type_full_name", "method_name" }
                    }
                }
            };
        }

        public CallToolResult ExecuteTool(string toolName, Dictionary<string, object>? arguments)
        {
            McpLogger.Info($"Executing tool: {toolName}");

            try
            {
                var result = toolName switch
                {
                    "list_tools" => ListTools(),
                    "list_assemblies" => InvokeLazy(assemblyTools, "ListAssemblies", null),
                    "get_assembly_info" => InvokeLazy(assemblyTools, "GetAssemblyInfo", arguments),
                    "list_types" => InvokeLazy(assemblyTools, "ListTypes", arguments),
                    "get_type_info" => InvokeLazy(typeTools, "GetTypeInfo", arguments),
                    "decompile_method" => InvokeLazy(typeTools, "DecompileMethod", arguments),
                    "list_methods_in_type" => InvokeLazy(typeTools, "ListMethodsInType", arguments),
                    "list_properties_in_type" => InvokeLazy(typeTools, "ListPropertiesInType", arguments),
                    "get_method_signature" => InvokeLazy(typeTools, "GetMethodSignature", arguments),
                    "search_types" => SearchTypes(arguments),
                    "find_who_calls_method" => FindWhoCallsMethod(arguments),
                    "analyze_type_inheritance" => AnalyzeTypeInheritance(arguments),
                    "get_method_il" => InvokeLazy(typeTools, "GetMethodIL", arguments),
                    "get_method_il_bytes" => InvokeLazy(typeTools, "GetMethodILBytes", arguments),
                    "get_method_exception_handlers" => InvokeLazy(typeTools, "GetMethodExceptionHandlers", arguments),
                    _ => new CallToolResult
                    {
                        Content = new List<ToolContent> {
                            new ToolContent { Text = $"Unknown tool: {toolName}" }
                        },
                        IsError = true
                    }
                };

                return result;
            }
            catch (Exception ex)
            {
                McpLogger.Exception(ex, $"Error executing tool {toolName}");
                return new CallToolResult
                {
                    Content = new List<ToolContent> {
                        new ToolContent { Text = $"Error executing tool {toolName}: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }

        CallToolResult ListTools()
        {
            var tools = GetAvailableTools();
            var json = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = true });
            return new CallToolResult
            {
                Content = new List<ToolContent> { new ToolContent { Text = json } }
            };
        }

        CallToolResult InvokeLazy<T>(Lazy<T> lazy, string methodName, Dictionary<string, object>? arguments) where T : class
        {
            if (lazy == null)
                throw new ArgumentNullException(nameof(lazy));
            try
            {
                object? instance;
                try
                {
                    instance = lazy.Value;
                }
                catch (Exception ex)
                {
                    McpLogger.Exception(ex, "InvokeLazy: failed to construct lazy.Value");
                    return new CallToolResult
                    {
                        Content = new List<ToolContent> { new ToolContent { Text = $"InvokeLazy construction error: {ex.Message}" } },
                        IsError = true
                    };
                }

                if (instance == null)
                {
                    return new CallToolResult
                    {
                        Content = new List<ToolContent> { new ToolContent { Text = "InvokeLazy: instance is null" } },
                        IsError = true
                    };
                }

                var type = instance.GetType();
                var mi = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (mi == null)
                {
                    return new CallToolResult
                    {
                        Content = new List<ToolContent> { new ToolContent { Text = $"Method not found on {type.FullName}: {methodName}" } },
                        IsError = true
                    };
                }

                var parameters = mi.GetParameters();
                object?[] invokeArgs;

                if (parameters.Length == 0) {
                    invokeArgs = Array.Empty<object?>();
                }
                else if (arguments == null || arguments.Count == 0) {
                    return new CallToolResult
                    {
                        Content = new List<ToolContent> { new ToolContent { Text = $"Method {methodName} requires arguments but none were provided" } },
                        IsError = true
                    };
                }
                else {
                    invokeArgs = new object?[] { arguments };
                }

                var res = mi.Invoke(instance, invokeArgs);
                if (res is CallToolResult ctr)
                    return ctr;

                var text = JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true });
                return new CallToolResult { Content = new List<ToolContent> { new ToolContent { Text = text } } };
            }
            catch (TargetInvocationException tie)
            {
                McpLogger.Exception(tie, $"InvokeLazy target invocation failed for {methodName}");
                return new CallToolResult
                {
                    Content = new List<ToolContent> { new ToolContent { Text = $"InvokeLazy invocation error: {tie.InnerException?.Message ?? tie.Message}" } },
                    IsError = true
                };
            }
            catch (Exception ex)
            {
                McpLogger.Exception(ex, "InvokeLazy failed");
                return new CallToolResult
                {
                    Content = new List<ToolContent> { new ToolContent { Text = $"InvokeLazy error: {ex.Message}" } },
                    IsError = true
                };
            }
        }

        dnlib.DotNet.AssemblyDef? FindAssemblyByName(string name)
        {
            return documentTreeView.GetAllModuleNodes()
                .Select(m => m.Document?.AssemblyDef)
                .FirstOrDefault(a => a?.Name.String.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        dnlib.DotNet.TypeDef? FindTypeInAssembly(dnlib.DotNet.AssemblyDef assembly, string typeFullName)
        {
            return assembly.Modules
                .SelectMany(m => m.Types)
                .FirstOrDefault(t => t.FullName.Equals(typeFullName, StringComparison.OrdinalIgnoreCase));
        }

        CallToolResult SearchTypes(Dictionary<string, object>? arguments)
        {
            if (arguments == null || !arguments.TryGetValue("query", out var queryObj))
                throw new ArgumentException("query is required");

            var query = queryObj.ToString() ?? string.Empty;

            string? cursor = null;
            if (arguments.TryGetValue("cursor", out var cursorObj))
                cursor = cursorObj.ToString();

            var (offset, pageSize) = DecodeCursor(cursor);

            bool hasWildcard = query.Contains("*");
            System.Text.RegularExpressions.Regex? regex = null;

            if (hasWildcard)
            {
                var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(query).Replace("\\*", ".*") + "$";
                regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            var results = documentTreeView.GetAllModuleNodes()
                .SelectMany(m => m.Document?.AssemblyDef != null ? m.Document.AssemblyDef.Modules.SelectMany(mod => mod.Types) : Enumerable.Empty<dnlib.DotNet.TypeDef>())
                .Where(t => {
                    if (regex != null)
                        return regex.IsMatch(t.FullName);
                    return t.FullName.Contains(query, StringComparison.OrdinalIgnoreCase);
                })
                .Select(t => new
                {
                    FullName = t.FullName,
                    Namespace = t.Namespace.String,
                    Name = t.Name.String,
                    AssemblyName = t.Module.Assembly?.Name.String ?? "Unknown"
                })
                .ToList();

            return CreatePaginatedJsonResponse(results, offset, pageSize);
        }

        CallToolResult FindWhoCallsMethod(Dictionary<string, object>? arguments)
        {
            if (arguments == null)
                throw new ArgumentException("Arguments are required");

            if (!arguments.TryGetValue("assembly_name", out var asmNameObj) ||
                !arguments.TryGetValue("type_full_name", out var typeNameObj) ||
                !arguments.TryGetValue("method_name", out var methodNameObj))
                throw new ArgumentException("assembly_name, type_full_name, and method_name are required");

            var assembly = FindAssemblyByName(asmNameObj.ToString() ?? "");
            if (assembly == null)
                throw new ArgumentException($"Assembly not found: {asmNameObj}");

            var type = FindTypeInAssembly(assembly, typeNameObj.ToString() ?? "");
            if (type == null)
                throw new ArgumentException($"Type not found: {typeNameObj}");

            var targetMethod = type.Methods.FirstOrDefault(m => m.Name.String == methodNameObj.ToString());
            if (targetMethod == null)
                throw new ArgumentException($"Method not found: {methodNameObj}");

            return usageFindingCommandTools.Value.FindWhoCallsMethod(targetMethod);
        }

        CallToolResult AnalyzeTypeInheritance(Dictionary<string, object>? arguments)
        {
            if (arguments == null)
                throw new ArgumentException("Arguments are required");

            if (!arguments.TryGetValue("assembly_name", out var asmNameObj) ||
                !arguments.TryGetValue("type_full_name", out var typeNameObj))
                throw new ArgumentException("assembly_name and type_full_name are required");

            var assembly = FindAssemblyByName(asmNameObj.ToString() ?? "");
            if (assembly == null)
                throw new ArgumentException($"Assembly not found: {asmNameObj}");

            var type = FindTypeInAssembly(assembly, typeNameObj.ToString() ?? "");
            if (type == null)
                throw new ArgumentException($"Type not found: {typeNameObj}");

            var baseClasses = new List<string>();
            var currentType = type.BaseType;
            while (currentType != null && currentType.FullName != "System.Object")
            {
                baseClasses.Add(currentType.FullName);
                var typeDef = currentType.ResolveTypeDef();
                currentType = typeDef?.BaseType;
            }

            var interfaces = type.Interfaces.Select(i => i.Interface.FullName).ToList();

            var result = JsonSerializer.Serialize(new
            {
                Type = type.FullName,
                BaseClasses = baseClasses,
                Interfaces = interfaces
            }, new JsonSerializerOptions { WriteIndented = true });

            return new CallToolResult
            {
                Content = new List<ToolContent> { new ToolContent { Text = result } }
            };
        }

        static (int offset, int pageSize) DecodeCursor(string? cursor)
        {
            if (string.IsNullOrEmpty(cursor))
                return (0, 10);

            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(cursor));
                var parts = decoded.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var offset) && int.TryParse(parts[1], out var pageSize))
                    return (offset, pageSize);
            }
            catch { }
            return (0, 10);
        }

        static string EncodeCursor(int offset, int pageSize)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{offset}:{pageSize}"));
        }

        static CallToolResult CreatePaginatedJsonResponse<T>(List<T> items, int offset, int pageSize)
        {
            var pagedItems = items.Skip(offset).Take(pageSize).ToList();
            var hasMore = offset + pageSize < items.Count;

            var result = new Dictionary<string, object>
            {
                ["items"] = pagedItems,
                ["total_count"] = items.Count,
                ["returned_count"] = pagedItems.Count,
                ["offset"] = offset
            };

            if (hasMore)
                result["nextCursor"] = EncodeCursor(offset + pageSize, pageSize);

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new CallToolResult
            {
                Content = new List<ToolContent> { new ToolContent { Text = json } }
            };
        }
    }
}
