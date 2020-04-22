using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dll2Sdk.Generators;
using Dll2Sdk.Utils;
using dnlib.DotNet;
using Nito.Collections;

namespace Dll2Sdk
{
    public class SdkGenerator
    {
        public SdkGenerator(ModuleDefMD module, IAssembly corlib)
        {
            Console.WriteLine("Generating types for: " + module);
            var visitedTypes = new HashSet<TypeDef>();
            var toVisit = new Deque<TypeDef>();
            foreach (TypeDef type in module.Types)
            {
                toVisit.AddToBack(type);
            }
            var inOrderGenerators = new List<TypeSdkGenerator>();
            while (toVisit.Count > 0)
            {
                TypeDef currentVisited = toVisit.RemoveFromBack();
                if (visitedTypes.Contains(currentVisited))
                    continue;
                bool canVisit = true;
                void CheckDep(TypeSig ts)
                {
                    if (ts?.IsValueType == true && !ts.IsPrimitive)
                    {
                        //Console.WriteLine(ts.FullName + " is non primitive and is struct?");
                        foreach (TypeDef t2 in ts.UsedTypes())
                        {
                            if (ts.TypeName.Equals("TypeCode"))
                                continue;
                            TypeDef tn = t2.GetNonNestedTypeRefScope().ResolveTypeDef();
                            if (tn != null && tn != currentVisited && tn.DefinitionAssembly == module.Assembly && !visitedTypes.Contains(tn))
                            {
                                canVisit = false;
                                toVisit.AddToBack(tn);
                            }
                        }
                    }
                }
                TypeDef baseType = currentVisited.BaseType?.ResolveTypeDef();
                if (baseType != null && baseType.DefinitionAssembly == module.Assembly && !visitedTypes.Contains(baseType))
                {
                    canVisit = false;
                    toVisit.AddToBack(baseType);
                }
                foreach (FieldDef valueField in currentVisited.Fields)
                {
                    CheckDep(valueField.FieldType);
                }
                foreach (MethodDef method in currentVisited.Methods)
                {
                    foreach (Parameter arg in method.Parameters)
                    {
                        CheckDep(arg.Type);
                    }
                    if (method.HasGenericParameters)
                    {
                        CheckDep(method.ReturnType);
                    }
                }
                if (!canVisit)
                {
                    toVisit.AddToFront(currentVisited);
                    continue;
                }
                visitedTypes.Add(currentVisited);
                inOrderGenerators.Add(new TypeSdkGenerator(currentVisited, currentVisited.ParsedFullNamespace()));
            }
            var dependencies = new HashSet<IAssembly>();
            void AddDependency(IAssembly assemblyRef)
            {
                if (assemblyRef == module.Assembly)
                {
                    return;
                }
                if (assemblyRef.Name.Contains("System.Private.CoreLib"))
                {
                    return;
                }
                dependencies.Add(assemblyRef);
            }
            void AddTypeDependency(ITypeDefOrRef typeDefOrRef)
            {
                if (typeDefOrRef == null)
                {
                    return;
                }
                if (typeDefOrRef.IsValueType && !typeDefOrRef.ResolveTypeDefThrow().IsEnum)
                {
                    AddDependency(typeDefOrRef.DefinitionAssembly);
                }
            }
            AddDependency(corlib);
            foreach (TypeDef dep in module.GetTypes().Where(t => !t.IsInterface))
            {
                if (dep.BaseType != null && !dep.IsEnum)
                {
                    AddDependency(dep.BaseType.DefinitionAssembly);
                }
                foreach (FieldDef field in dep.Fields)
                {
                    AddTypeDependency(field.FieldType.GetNonNestedTypeRefScope());
                }
                foreach (MethodDef method in dep.Methods)
                {
                    AddTypeDependency(method.ReturnType.GetNonNestedTypeRefScope());
                    foreach (Parameter param in method.Parameters)
                    {
                        AddTypeDependency(param.Type.GetNonNestedTypeRefScope());
                    }
                }
            }
            var deps = new HashSet<string>();
            var depBuilder = new StringBuilder();
            foreach (IAssembly d in dependencies)
            {
                string dn = d.Name.String.Parseable();
                if (deps.Add(dn))
                {
                    depBuilder.AppendLine($"#include \"..\\{dn}\\{dn}.hpp\"");
                }
            }
            var hdr = new IndentedBuilder();
            var forward = new IndentedBuilder();
            var file = new IndentedBuilder();
            foreach (TypeSdkGenerator generator in inOrderGenerators)
            {
                forward.AppendIndented("namespace ");
                forward.Append(generator.Namespace);
                forward.AppendNewLine();       
                forward.AppendIndentedLine("{");
                forward.Indent();            
                generator.GenerateForwardTypeDefinition(forward);        
                forward.Outdent();
                forward.AppendIndentedLine("}\n");
                if (!generator.TypeDef.IsInterface)
                {
                    hdr.AppendIndented("namespace ");
                    hdr.Append(generator.Namespace);
                    hdr.AppendNewLine();          
                    hdr.AppendIndentedLine("{");
                    hdr.Indent();            
                    generator.GenerateHeaderTypeDefinition(hdr);       
                    hdr.Outdent();
                    hdr.AppendIndentedLine("}\n");
                }
                generator.GenerateImplementation(file);
            }
            string name = module.Assembly.Name.String.Parseable();
            string path = $"{Program.Arguments.OutDirectory}";
            Directory.CreateDirectory($"{path}/{name}");
            File.WriteAllText($"{path}/{name}/{name}.hpp", $@"//generated with dll2sdk
#pragma once
#include ""..\dll2sdk_forward.g.hpp""
{depBuilder}
{hdr}");
            File.WriteAllText($"{path}/{name}/{name}_forward.hpp", $@"//generated with dll2sdk
#pragma once
#include ""..\dll2sdk_forward.g.hpp""

{forward}");
            File.WriteAllText($"{path}/{name}/{name}.cpp", $@"//generated with dll2sdk
#include ""{name}.hpp""

{file}");
            File.AppendAllText($"{path}/dll2sdk_forward.g.hpp", $@"#include ""{name}\{name}_forward.hpp""
");
        }
    }
}
