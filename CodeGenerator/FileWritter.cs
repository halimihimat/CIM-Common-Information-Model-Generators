using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator
{
    public class FileWritter
    {
        private CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

        public FileWritter()
        {

        }
        public void WriteFiles(string assemblyVersion,List<CodeCompileUnit> Files,string path,bool isDB)
        {
            int counter = 0;
            foreach (CodeCompileUnit unit in Files)
            {
                if (unit != null)
                {
                   /* if (unit.Namespaces[0].Types[0].Name.Equals("IDClass"))
                    {
                        CodeTypeReference attr = new CodeTypeReference("AssemblyVersion");
                        CodeAttributeDeclaration decl = new CodeAttributeDeclaration(attr, new CodeAttributeArgument(new CodePrimitiveExpression(assemblyVersion)));
                        unit.AssemblyCustomAttributes.Add(decl);
                    }*/
                    if (!isDB)
                    {
                        String sourceFile;
                        if (provider.FileExtension[0] == '.')
                        {
                            sourceFile = path + "\\classes\\" + unit.Namespaces[0].Types[0].Name + provider.FileExtension;
                        }
                        else
                        {
                            sourceFile = path + "\\classes\\" + unit.Namespaces[0].Types[0].Name + "." + provider.FileExtension;
                        }
                        if (!System.IO.Directory.Exists(path + "\\classes\\"))
                        {
                            System.IO.Directory.CreateDirectory(path + "\\classes\\");
                        }
                        IndentedTextWriter tw = new IndentedTextWriter(new StreamWriter(sourceFile, false), "    ");
                        provider.GenerateCodeFromCompileUnit(unit, tw, new CodeGeneratorOptions());
                        tw.Close();
                        counter++;
                    }
                    else
                    {
                        String sourceFile;
                        if (provider.FileExtension[0] == '.')
                        {
                            sourceFile = path + "\\classes\\Model\\" + unit.Namespaces[0].Types[0].Name + provider.FileExtension;
                        }
                        else
                        {
                            sourceFile = path + "\\classes\\Model\\" + unit.Namespaces[0].Types[0].Name + "." + provider.FileExtension;
                        }
                        if (!System.IO.Directory.Exists(path + "\\classes\\Model\\"))
                        {
                            System.IO.Directory.CreateDirectory(path + "\\classes\\Model\\");
                        }
                        IndentedTextWriter tw = new IndentedTextWriter(new StreamWriter(sourceFile, false), "    ");
                        provider.GenerateCodeFromCompileUnit(unit, tw, new CodeGeneratorOptions());
                        tw.Close();
                        counter++;
                    }
                }
            }
           
        }
    }
}
