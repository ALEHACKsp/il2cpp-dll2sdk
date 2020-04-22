using System;
using System.Linq;
using Dll2Sdk.Utils;
using dnlib.DotNet;

namespace Dll2Sdk.Generators
{
    public class TypeSdkGenerator
    {
        public TypeDef TypeDef;
        public string Namespace;

        public void GenerateForwardTypeDefinition(IndentedBuilder builder)
        {
            builder.AppendIndented(TypeDef.TypeDefinitionStr());
            if (TypeDef.IsEnum)
            {
                builder.Append(" : ");
                builder.Append(TypeDef.GetEnumUnderlyingType().ParsedTypeSignatureStr());
            }
            builder.Append(";");
            builder.AppendNewLine();
        }

        public void GenerateHeaderTypeDefinition(IndentedBuilder builder)
        {
            builder.AppendIndented(TypeDef.TypeDefinitionStr());
            if (TypeDef.IsEnum)
            {
                string tss = TypeDef.GetEnumUnderlyingType().ParsedTypeSignatureStr();               
                builder.Append(" : ");
                builder.AppendLine(tss);
                builder.AppendIndentedLine("{");
                builder.Indent();
                for (int index = 0; index < TypeDef.Fields.Count; index++)
                {
                    FieldDef field = TypeDef.Fields[index];
                    if (!field.IsSpecialName)
                    {
                        builder.AppendIndented($"{field.Name.String.Parseable()}_");
                        if (field.HasConstant)
                        {
                            builder.Append(" = ");
                            builder.Append($"static_cast<{tss}>(0x{field.Constant.Value:X})");
                        }
                        if (index < TypeDef.Fields.Count - 1)
                        {
                            builder.AppendLine(",");
                        }
                    }
                }
                builder.AppendNewLine();
            }
            else
            {
                if (!TypeDef.IsValueType && !TypeDef.IsInterface)
                {
                    builder.Append(" : ");
                    builder.Append(TypeDef.BaseType?.ToTypeSig().ParsedTypeSignatureStr() ?? "DLL2SDK::Object");
                }
                builder.AppendNewLine();
                builder.AppendIndentedLine("{");
                builder.Indent();              
                if (TypeDef.HasNestedTypes)
                {
                    foreach (TypeDef type in TypeDef.NestedTypes)
                    {
                        new TypeSdkGenerator(type, Namespace).GenerateHeaderTypeDefinition(builder);
                    }
                }
                if (TypeDef.IsExplicitLayout)
                {
                    builder.AppendIndentedLine("union");
                    builder.AppendIndentedLine("{");
                    builder.Indent();
                    int i = 0;
                    foreach (FieldDef field in TypeDef.Fields)
                    {
                        if (!field.IsStatic)
                        {
                            builder.AppendIndentedLine("struct");
                            builder.AppendIndentedLine("{");
                            builder.Indent();
                            //so in other news c++ sucks
                            object offset = new System.ComponentModel.Int32Converter().ConvertFromString(field
                                .CustomAttributes
                                .First(a => a.TypeFullName.Contains("FieldOffset"))
                                .GetNamedArgument("Offset", true).Value.ToString());
                            if (offset != null && (int)offset != 0)
                            {
                                builder.AppendIndentedLine($"uint8_t offset_{++i}[0x{offset:X}];");
                            }
                            builder.AppendIndentedLine(field.ParsedTypeDefinitionStr());
                            
                            builder.Outdent();
                            builder.AppendIndentedLine("};");
                        }
                    }
                    builder.Outdent();
                    builder.AppendIndentedLine("};");
                }
                else
                {
                    foreach (FieldDef field in TypeDef.Fields)
                    {
                        if (!field.IsStatic)
                        {
                            builder.AppendIndentedLine(field.ParsedTypeDefinitionStr());
                        }
                    }
                }
                FieldDef[] staticFields = TypeDef.Fields.Where(f => f.IsStatic).ToArray();
                if (staticFields.Length > 0)
                {
                    if (TypeDef.IsValueType)
                    {
                        builder.AppendIndentedLine("/* NOTE: structure has static fields; this is not yet supported.");
                    }
                    builder.AppendIndentedLine("struct StaticFields");
                    builder.AppendIndentedLine("{");
                    builder.Indent();                 
                    foreach (FieldDef field in staticFields)
                    {
                        builder.AppendIndentedLine(field.ParsedTypeDefinitionStr());
                    }
                    builder.Outdent();
                    builder.AppendIndentedLine("};");
                    builder.AppendIndentedLine("StaticFields* GetStaticFields() { return reinterpret_cast<StaticFields*>(this->ClassPtr->StaticFieldsPtr); }");
                    if (TypeDef.IsValueType)
                    {
                        builder.AppendIndentedLine("*/");
                    }
                }
            }
            if (TypeDef.HasMethods)
            {
                MethodDef[] instancedMethods = TypeDef.Methods.Where(m => (!m.IsVirtual || !m.IsInterfacedMethod()) && !m.IsStatic).ToArray();
                MethodDef[] staticMethods = TypeDef.Methods.Where(m => m.IsStatic && m.Name != "op_Explicit" && m.Name != "op_Implicit").ToArray();
                foreach (MethodDef instancedMethod in instancedMethods)
                {
                    builder.AppendIndentedLine(instancedMethod.DeclarationStr());
                }
                foreach (MethodDef staticMethod in staticMethods)
                {
                    builder.AppendIndentedLine(staticMethod.DeclarationStr());
                }
            }
            builder.Outdent();
            builder.AppendIndentedLine("};");
        }

        public void GenerateImplementation(IndentedBuilder builder)
        {
            foreach (MethodDef m in TypeDef.Methods.Where(m => (!m.IsVirtual || !m.IsInterfacedMethod()) && m.Name != "op_Explicit" && m.Name != "op_Implicit"))
            {
                object rva = m.GetRva();
                if (!m.HasGenericParameters && rva != null)
                {
                    string rst = m.ImplementationStr(rva).Replace("{" ,"\n{\n\t");
                    rst = rst.Replace(";", ";\n\t");
                    rst = rst.Replace("	 }", "}\n");
                    builder.AppendIndentedLine(rst.Trim() + Environment.NewLine);
                }
            }
        }
        
        public TypeSdkGenerator(TypeDef typeDef, string parsedNamespace)
        {
            TypeDef = typeDef;
            Namespace = parsedNamespace;
        }
    }
}
