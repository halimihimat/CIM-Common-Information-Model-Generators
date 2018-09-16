using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Common;
using System.Windows.Forms;
using System.IO;
using CodeGenerator.Model;
using System.Threading;
using System.CodeDom;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

namespace CodeGenerator.ViewModel
{


    public class MainWindowViewModel : INotifyPropertyChanged
    {

        public Thread t1;
        private string filePath;
        private string destPath;
        private string logTb;
        private string dbName;
        private List<CodeCompileUnit> filesComplete = new List<CodeCompileUnit>();
        private List<CodeCompileUnit> filesforDb = new List<CodeCompileUnit>();
        public bool isSelected;
        private ICommand openFileDialog;
        private ICommand generateCommand;
        private ICommand destFileDialog;

        #region Properties
        public bool IsDbSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsDbSelected");
            }
        }

        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                OnPropertyChanged("FilePath");
            }
        }
        public string DestPath
        {
            get { return destPath; }
            set
            {
                destPath = value;
                OnPropertyChanged("DestPath");
            }
        }
        public string DbName
        {
            get { return dbName; }
            set
            {
                dbName = value;
                OnPropertyChanged("DbName");
            }
        }

        public string LogTB
        {
            get { return logTb; }
            set
            {
                logTb = value;
                OnPropertyChanged("LogTB");
            }
        }

        private List<CodeCompileUnit> FilesComplete
        {
            get
            {
                return filesComplete;
            }
            set
            {
                filesComplete = value;
            }
        }

        private List<CodeCompileUnit> FilesForDb
        {
            get
            {
                return filesforDb;
            }
            set
            {
                filesforDb = value;
            }
        }

        public ICommand OpenFileDialog
        {
            get
            {
                return openFileDialog ?? (openFileDialog = new RelayCommand(param => this.OpenFile()));
            }
        }

        public ICommand DestFileCommand
        {
            get
            {
                return destFileDialog ?? (destFileDialog = new RelayCommand(param => this.DestFile()));
            }
        }



        public ICommand GenerateCommand
        {
            get
            {
                return generateCommand ?? (generateCommand = new RelayCommand(param => this.GenCommand()));
            }
        }

        #endregion

        public MainWindowViewModel()
        {

        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open EA File..";
            openFileDialog.Filter = "Enterpise Architect Files|*.eap;|All Files|*.*";
            openFileDialog.RestoreDirectory = true;

            DialogResult dialogResponse = openFileDialog.ShowDialog();
            if (dialogResponse == DialogResult.OK)
            {
                FilePath = openFileDialog.FileName;
            }
        }

        private void DestFile()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    DestPath = fbd.SelectedPath;
                }
            }
        }

        private void GenCommand()
        {

            string name = "";
            if (FilePath == null || DestPath == null)
            {
                LogTB = "File path or Destination path is not selected!";
                return;
            }
            try
            {
                name = Path.GetFileNameWithoutExtension(FilePath);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                LogTB = "EAP Directory does not exist!" + " Not completed!";
                return;
            }
            catch (Exception e)
            {
                LogTB = "Exception message:\n" + e.Message + " Not completed!";
                return;
            }
            if (IsDbSelected && DbName == null  )
            {
                LogTB = "Database name length must be in range 1 to 20 characters";
                return;
            }

           
            Thread t = new Thread((ThreadStart)delegate ()
            {
                ProcessModel(filePath, name);
            });
            t.Start();

            t1 = new Thread((ThreadStart)delegate ()
             {
                 WaitingMethod();
             });
            t1.Start();


        }
        public void ProcessModel(string input, string name)
        {
            string txtFile = "";
            string output = "";
            string err = "";
            EAPModelReader modelReader = new EAPModelReader(ref txtFile, name, output);
            EAPModels eapModels = modelReader.CollectClasses(input, ref err);
            if (eapModels == null)
            {
                t1.Abort();
                LogTB = "Invalid file format!";
                return;
            }
            FileWritter fw = new FileWritter();
            EAPModel[] eapObjModels = eapModels.Models.ToArray();


            GenerateCode(eapObjModels[0]);
            fw.WriteFiles("1.0.0", FilesComplete, DestPath, false);
            fw.WriteFiles("1.0.0", FilesForDb, DestPath, true);
            t1.Abort();
            var path = DestPath + "\\classes";
            Process.Start(@path);
            LogTB = "\nFINISHED!";
        }
        public void GenerateCode(EAPModel model)
        {
            FilesComplete.Clear();
            FilesForDb.Clear();
            foreach (var item in model.Classes)
            {
                DictionaryEntry table = (DictionaryEntry)item;

                CodeCompileUnit unit = BuildCodeCUnit(model, table.Value, true);
                if (IsDbSelected)
                {
                    CodeCompileUnit unitDb = BuildCodeCUnit(model, table.Value, false);
                    if (unitDb != null)
                        FilesForDb.Add(unitDb);
                }
                if (unit != null)
                {
                    //add it to the list, unless its null
                    FilesComplete.Add(unit);
                }
            }
            if (IsDbSelected)
            {
                CodeCompileUnit unitDb = CreateDBContextClass(model);
                FilesForDb.Add(unitDb);
            }
         //   CreateParentClass(); // ovo treba izbaciti za IdentifiedObject
        }

        private CodeCompileUnit CreateDBContextClass(EAPModel model)
        {
            CodeCompileUnit unit = new CodeCompileUnit();
            //namespace
            CodeNamespace nameSpace = new CodeNamespace("Default_Namespace");
            unit.Namespaces.Add(nameSpace);
            //namespace imports
            nameSpace.Imports.Add(new CodeNamespaceImport("System"));

            CodeTypeDeclaration file = new CodeTypeDeclaration();
            file.IsClass = true;
            file.Name = DbName;
            file.TypeAttributes = TypeAttributes.Public;
            file.Attributes = MemberAttributes.Public;
            nameSpace.Types.Add(file);

            return unit;


        }

        private CodeCompileUnit BuildCodeCUnit(EAPModel model, object v, bool flagDb)
        {
            CodeCompileUnit unit = null;
            EAPClass classPom = (EAPClass)v;
            unit = CreateClass(model, classPom, flagDb);

            return unit;
        }

        private CodeCompileUnit CreateClass(EAPModel model, EAPClass classPom, bool flagDb)
        {
            CodeCompileUnit unit = new CodeCompileUnit();
            //namespace
            CodeNamespace nameSpace = new CodeNamespace("Default_Namespace");
            unit.Namespaces.Add(nameSpace);
            //namespace imports
            nameSpace.Imports.Add(new CodeNamespaceImport("System"));

            CodeTypeDeclaration file = new CodeTypeDeclaration();
            file.IsClass = true;
            file.Name = classPom.Name;
            file.TypeAttributes = TypeAttributes.Public;
            file.Attributes = MemberAttributes.Public;

            if (classPom.Parent != null)
            {
                file.BaseTypes.Add(new CodeTypeReference(classPom.Parent));

                nameSpace.Imports.Add(new CodeNamespaceImport("Default_Namespace"));
            }
            else
            {
                //if class doesn't have a parent,
                //it should extend IDClass as the root of hierarhy - for rdf:ID
                file.BaseTypes.Add(new CodeTypeReference("IDClass"));
                nameSpace.Imports.Add(new CodeNamespaceImport("Default_Namespace"));  
            }

            if (!string.IsNullOrEmpty(classPom.Description))
            {
                file.Comments.Add(new CodeCommentStatement(classPom.Description, true));
            }
            //Generate constructor without param
            CodeConstructor baseStringConstructorSimple = new CodeConstructor();
            baseStringConstructorSimple.Attributes = MemberAttributes.Public;

            //Gererating constuctor with param
            CodeConstructor baseStringConstructor = new CodeConstructor();
            baseStringConstructor.Attributes = MemberAttributes.Public;
            baseStringConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("globalId"));
            baseStringConstructor.Parameters.Add(new CodeParameterDeclarationExpression("System.Int64", "globalId"));

            file.Members.Add(baseStringConstructorSimple);
            file.Members.Add(baseStringConstructor);

            List<EAPAttribute> attributes = new List<EAPAttribute>();


            if (classPom.Attributes.Count > 0)
            {
                foreach (EAPAttribute attribut in classPom.Attributes)
                {
                    CodeMemberField att = null;
                    if (attribut.MeasurementType != "")
                    {
                        if (Enum.IsDefined(typeof(MeasurementType), attribut.MeasurementType.ToUpper()) || attribut.MeasurementType.Contains("List<") || attribut.TypeCode.Equals("Enum")
                            || attribut.TypeCode.Equals("Class"))
                        {
                            if (attribut.Max == "*")
                            {
                                nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

                                string fieldName = attribut.Name;

                                att = new CodeMemberField(new CodeTypeReference("List", new CodeTypeReference[] { new CodeTypeReference(attribut.MeasurementType.Split('<', '>')[1]) }), fieldName);
                                att.Attributes = MemberAttributes.Private | MemberAttributes.Final;
                                att.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference("List", new CodeTypeReference[] { new CodeTypeReference(attribut.MeasurementType.Split('<', '>')[1]) }));
                                if (!string.IsNullOrEmpty(attribut.Description))
                                {
                                    att.Comments.Add(new CodeCommentStatement(attribut.Description, true));
                                }
                                file.Members.Add(att);

                                CreatePropertyForField(file, att, true, true);
                            }
                            else
                            {

                                string fieldName = attribut.Name;
                                string type = attribut.MeasurementType;
                                // att = new CodeMemberField(StringManipulationManager.GetSystemType(type), fieldName);
                                if (attribut.TypeCode != null && attribut.TypeCode != "")
                                {
                                    if (attribut.TypeCode.Equals("Enum"))
                                    {
                                        att = new CodeMemberField(attribut.MeasurementType, fieldName);
                                    }
                                    if (attribut.TypeCode.Equals("Class"))
                                    {
                                        att = new CodeMemberField(attribut.MeasurementType, fieldName);
                                    }
                                }
                                else
                                {
                                    att = new CodeMemberField(StringManipulationManager.GetSystemType(type), fieldName);
                                }

                                att.Attributes = MemberAttributes.Private | MemberAttributes.Final;
                                if (!string.IsNullOrEmpty(attribut.Description))
                                {
                                    att.Comments.Add(new CodeCommentStatement(attribut.Description, true));
                                }
                                if (type.Equals("long") && attribut.IsReference == true)
                                {
                                    att.InitExpression = new CodeSnippetExpression("0");
                                }
                                file.Members.Add(att);

                                //property for the field
                                CreatePropertyForField(file, attribut, att, true, true);

                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                if (IsDbSelected && flagDb == false)
                {
                    GenerateIDForDb(file,classPom.Name);   // generate id if db checbox is selected
                }
                if (flagDb)
                {
                    GenerateEqualsMethod(file, classPom);
                    GenerateHashCodeMethod(file, classPom);
                    GenerateGetProperty(file, classPom);
                    GenerateHasPropertyMethod(file, classPom);
                    GenerateSetPropertyMethod(file, classPom);
                    GenerateIsReferencedProperty(file, classPom);
                    GenerateGetReferencesMethod(file, classPom);
                    GenerateAddreferenceMethod(file, classPom);
                    GenerateRemoveReferencMethod(file, classPom);
                }
            }
            else
            {
                if (flagDb)
                {
                    GenerateEqualsMethod(file, classPom);
                    GenerateHashCodeMethod(file, classPom);
                    GenerateGetProperty(file, classPom);
                    GenerateHasPropertyMethod(file, classPom);
                    GenerateSetPropertyMethod(file, classPom);
                    GenerateGetReferencesMethod(file, classPom);
                }
            }

            nameSpace.Types.Add(file);
            return unit;
        }

        private void GenerateIDForDb(CodeTypeDeclaration file,string className)
        {
            CodeMemberField prop = new CodeMemberField();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            prop.Type = new CodeTypeReference(typeof(int));

            prop.Name = className+"ID";
            CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration(
                "Key");
            prop.CustomAttributes.Add(codeAttrDecl);

            file.Members.Add(prop);
        }

        private void GenerateRemoveReferencMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "RemoveReference";
            method.ReturnType = new CodeTypeReference(typeof(void));
            method.Parameters.Add(new CodeParameterDeclarationExpression("ModelCode", "referenceId"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(long), "globalId"));


            CodeSnippetStatement switctS = new CodeSnippetStatement("\t\t\t\tswitch(referenceId)");
            CodeSnippetStatement bracket = new CodeSnippetStatement("\t\t\t\t{");
            method.Statements.Add(switctS); method.Statements.Add(bracket);
            foreach (EAPAttribute att in classPom.Attributes)
            {
                if (att.IsListOfReferences == true)
                {
                    CodeSnippetStatement mc = new CodeSnippetStatement("\t\t\t\t\tcase ModelCode." + att.onSameConnectorPairAtt.Code + ":");
                    CodeSnippetStatement ifSt = new CodeSnippetStatement("\t\t\t\t\t\tif (" + att.Name + ".Contains(globalId))");
                    CodeSnippetStatement openBr = new CodeSnippetStatement("\t\t\t\t\t\t{");

                    CodeStatement trueStatement = new CodeSnippetStatement("\t\t\t\t\t\t\t" + att.Name + ".Remove(globalId);");
                    CodeSnippetStatement closeBrec = new CodeSnippetStatement("\t\t\t\t\t\t}");
                    CodeSnippetStatement elseSt = new CodeSnippetStatement("\t\t\t\t\t\telse");



                    CodeSnippetStatement openBrElse = new CodeSnippetStatement("\t\t\t\t\t\t{");
                    CodeStatement falseStatement = new CodeSnippetStatement("\t\t\t\t\t\t\tCommonTrace.WriteTrace(CommonTrace.TraceWarning, \"Entity(GID = 0x{ 0:x16}) doesn't contain reference 0x{1:x16}.\", this.GlobalId, globalId);");
                    CodeSnippetStatement closeBrecElse = new CodeSnippetStatement("\t\t\t\t\t\t}");
                    CodeSnippetStatement breakSnipet = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");

                    method.Statements.Add(mc); method.Statements.Add(ifSt); method.Statements.Add(openBr);
                    method.Statements.Add(trueStatement); method.Statements.Add(closeBrec); method.Statements.Add(elseSt);
                    method.Statements.Add(openBrElse); method.Statements.Add(falseStatement); method.Statements.Add(closeBrecElse); method.Statements.Add(breakSnipet);
                }
            }
            CodeSnippetStatement defaultS = new CodeSnippetStatement("\t\t\t\t\tdefault:");
            CodeSnippetStatement baseS = new CodeSnippetStatement("\t\t\t\t\t\t base.RemoveReference(referenceId, globalId);");
            CodeSnippetStatement breakS = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
            CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
            method.Statements.Add(defaultS); method.Statements.Add(baseS); method.Statements.Add(breakS); method.Statements.Add(ccss);

            file.Members.Add(method);
        }

        private void GenerateAddreferenceMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "AddReference";
            method.ReturnType = new CodeTypeReference(typeof(void));
            method.Parameters.Add(new CodeParameterDeclarationExpression("ModelCode", "referenceId"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(long), "globalId"));


            CodeSnippetStatement switctS = new CodeSnippetStatement("\t\t\t\tswitch(referenceId)");
            CodeSnippetStatement bracket = new CodeSnippetStatement("\t\t\t\t{");
            method.Statements.Add(switctS); method.Statements.Add(bracket);


            foreach (EAPAttribute att in classPom.Attributes)
            {
                if (att.IsListOfReferences == true)
                {
                    CodeSnippetStatement mc = new CodeSnippetStatement("\t\t\t\t\tcase ModelCode." + att.onSameConnectorPairAtt.Code + ":");
                    CodeSnippetStatement add = new CodeSnippetStatement("\t\t\t\t\t\t" + att.Name + ".Add(globalId);");
                    CodeSnippetStatement breakSnipet = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
                    method.Statements.Add(mc); method.Statements.Add(add); method.Statements.Add(breakSnipet);
                }

            }
            CodeSnippetStatement defaultS = new CodeSnippetStatement("\t\t\t\t\tdefault:");
            CodeSnippetStatement baseS = new CodeSnippetStatement("\t\t\t\t\t\t base.AddReference(referenceId, globalId);");
            CodeSnippetStatement breakS = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
            CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
            method.Statements.Add(defaultS); method.Statements.Add(baseS); method.Statements.Add(breakS); method.Statements.Add(ccss);

            file.Members.Add(method);
        }

        private void GenerateGetReferencesMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "GetReferences";
            method.ReturnType = new CodeTypeReference(typeof(void));
            method.Parameters.Add(new CodeParameterDeclarationExpression("Dictionary<ModelCode, List<long>>", "references"));
            method.Parameters.Add(new CodeParameterDeclarationExpression("TypeOfReference", "refType"));

            foreach (EAPAttribute att in classPom.Attributes)
            {
                if (att.IsListOfReferences == true)
                {
                    CodeConditionStatement codeIf = new CodeConditionStatement(new
                           CodeSnippetExpression(att.Name + " != null && " + att.Name + ".Count > 0  && (refType == TypeOfReference.Target || refType == TypeOfReference.Both)"), new
                           CodeSnippetStatement("\t\t\t\treferences[ModelCode." + att.Code + "] = " + att.Name + ".GetRange(0, " + att.Name + ".Count);"));
                    method.Statements.Add(codeIf);
                }
                else if (att.IsReference == true)
                {
                    CodeStatement[] listTrueStatement = new CodeStatement[]
                    {
                        new CodeSnippetStatement("\t\t\t\treferences[ModelCode." + att.Code + "] =  new List<long>();"),
                        new CodeSnippetStatement("\t\t\t\treferences[ModelCode." + att.Code + "].Add("+att.Name+");")
                    };
                    CodeConditionStatement codeIf = new CodeConditionStatement(new
                          CodeSnippetExpression(att.Name + " != 0 && (refType == TypeOfReference.Reference || refType == TypeOfReference.Both)"), listTrueStatement);
                    method.Statements.Add(codeIf);
                }
            }
            method.Statements.Add(new CodeSnippetStatement("\t\t\tbase.GetReferences(references, refType);"));
            file.Members.Add(method);


        }

        private void GenerateIsReferencedProperty(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            prop.Type = new CodeTypeReference(typeof(bool));
            prop.Name = "IsReferenced";
            prop.HasGet = true;
            prop.GetStatements.Add(new CodeSnippetStatement("\t\t\t\t\treturn"));
            for (int i = 0; i < classPom.Attributes.Count; i++)
            {
                if (classPom.Attributes.ElementAt(i).IsListOfReferences == true)
                {
                    CodeSnippetStatement cs = new CodeSnippetStatement("\t\t\t\t\t\t (" + classPom.Attributes.ElementAt(i).Name + ".Count > 0) ||");
                    prop.GetStatements.Add(cs);
                }
            }
            prop.GetStatements.Add(new CodeSnippetExpression("\tbase.IsReferenced"));

            file.Members.Add(prop);
        }

        private void GenerateSetPropertyMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "SetProperty";
            method.ReturnType = new CodeTypeReference(typeof(void));
            method.Parameters.Add(new CodeParameterDeclarationExpression("Property", "property"));

            List<CodeSnippetStatement> snipets = new List<CodeSnippetStatement>()
            {
                new CodeSnippetStatement("\t\t\t\tswitch(property.Id)"),new CodeSnippetStatement("\t\t\t\t{")
            };
            foreach (EAPAttribute att in classPom.Attributes)
            {
                if (att.IsListOfReferences == true)
                    continue;
                CodeSnippetStatement css = new CodeSnippetStatement("\t\t\t\t\tcase ModelCode." + att.Code + ":");
                CodeSnippetStatement cssProp = new CodeSnippetStatement();
                if (att.TypeCode != "" && att.TypeCode.Equals("Enum"))
                {
                    cssProp = new CodeSnippetStatement("\t\t\t\t\t\t" + att.Name + " = (" + att.MeasurementType + ") property.AsEnum();");
                }
                else if (att.IsReference == true || att.TypeCode.Equals("Class"))
                {
                    cssProp = new CodeSnippetStatement("\t\t\t\t\t\t" + att.Name + " = property.AsReference();");
                }
                else
                {
                    cssProp = new CodeSnippetStatement("\t\t\t\t\t\t" + att.Name + " = property." + StringManipulationManager.GetAsMethod(att.MeasurementType));
                }
                CodeSnippetStatement cssBreak = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
                snipets.Add(css); snipets.Add(cssProp); snipets.Add(cssBreak);

            }
            CodeSnippetStatement cssDefault = new CodeSnippetStatement("\t\t\t\t\tdefault:");
            CodeSnippetStatement cssBase = new CodeSnippetStatement("\t\t\t\t\t\tbase.SetProperty(property);");
            CodeSnippetStatement cssBreakDefault = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
            CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
            snipets.Add(cssDefault); snipets.Add(cssBase); snipets.Add(cssBreakDefault); snipets.Add(ccss);
            foreach (var item in snipets)
            {
                method.Statements.Add(item);
            }

            file.Members.Add(method);
        }

        private void GenerateGetProperty(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "GetProperty";
            method.ReturnType = new CodeTypeReference(typeof(void));
            method.Parameters.Add(new CodeParameterDeclarationExpression("Property", "property"));

            List<CodeSnippetStatement> snipets = new List<CodeSnippetStatement>()
            {
                new CodeSnippetStatement("\t\t\t\tswitch(property.Id)"),new CodeSnippetStatement("\t\t\t\t{")
            };

            foreach (EAPAttribute att in classPom.Attributes)
            {
                CodeSnippetStatement css = new CodeSnippetStatement("\t\t\t\t\tcase ModelCode." + att.Code + ":");
                CodeSnippetStatement cssProp = new CodeSnippetStatement();
                if (att.TypeCode != "" && (att.TypeCode.Equals("Enum")))
                {
                    cssProp = new CodeSnippetStatement("\t\t\t\t\t\tproperty.SetValue((short)" + att.Name + ");");
                }
                else
                {
                    cssProp = new CodeSnippetStatement("\t\t\t\t\t\tproperty.SetValue(" + att.Name + ");");
                }
                CodeSnippetStatement cssBreak = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
                snipets.Add(css); snipets.Add(cssProp); snipets.Add(cssBreak);
            }
            CodeSnippetStatement cssDefault = new CodeSnippetStatement("\t\t\t\t\tdefault:");
            CodeSnippetStatement cssBase = new CodeSnippetStatement("\t\t\t\t\t\tbase.GetProperty(property);");
            CodeSnippetStatement cssBreakDefault = new CodeSnippetStatement("\t\t\t\t\t\tbreak;");
            CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
            snipets.Add(cssDefault); snipets.Add(cssBase); snipets.Add(cssBreakDefault); snipets.Add(ccss);
            foreach (var item in snipets)
            {
                method.Statements.Add(item);
            }

            file.Members.Add(method);

        }

        private void GenerateHasPropertyMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "HasProperty";
            method.ReturnType = new CodeTypeReference(typeof(bool));
            method.Parameters.Add(new CodeParameterDeclarationExpression("ModelCode", "t"));

            List<CodeSnippetStatement> snipets = new List<CodeSnippetStatement>()
            {
                new CodeSnippetStatement("\t\t\t\tswitch(t)"),new CodeSnippetStatement("\t\t\t\t{")
            };


            foreach (EAPAttribute att in classPom.Attributes)
            {
                CodeSnippetStatement css = new CodeSnippetStatement("\t\t\t\t\tcase ModelCode." + att.Code + ":");
                snipets.Add(css);
            }
            if (classPom.Attributes.Count > 0)
            {
                CodeSnippetStatement cssReturnT = new CodeSnippetStatement("\t\t\t\t\t\treturn true;");
                CodeSnippetStatement cssDefault = new CodeSnippetStatement("\t\t\t\t\tdefault:");
                CodeSnippetStatement cssReturnBase = new CodeSnippetStatement("\t\t\t\t\t\treturn base.HasProperty(t);");
                CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
                snipets.Add(cssReturnT); snipets.Add(cssDefault); snipets.Add(cssReturnBase); snipets.Add(ccss);
            }
            else
            {
                CodeSnippetStatement cssDefault = new CodeSnippetStatement("\t\t\t\t\tdefault:");
                CodeSnippetStatement cssReturnBase = new CodeSnippetStatement("\t\t\t\t\t\treturn base.HasProperty(t);");
                CodeSnippetStatement ccss = new CodeSnippetStatement("\t\t\t\t}");
                snipets.Add(cssDefault); snipets.Add(cssReturnBase); snipets.Add(ccss);
            }

            foreach (var item in snipets)
            {
                method.Statements.Add(item);
            }

            file.Members.Add(method);
        }

        private void GenerateHashCodeMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "GetHashCode";
            method.ReturnType = new CodeTypeReference(typeof(int));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("base.GetHashCode()")));
            file.Members.Add(method);
        }

        private void GenerateEqualsMethod(CodeTypeDeclaration file, EAPClass classPom)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            method.Name = "Equals";
            method.ReturnType = new CodeTypeReference(typeof(bool));
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "obj"));
            CodeExpression condition = new CodeBinaryOperatorExpression(
                   new CodeSnippetExpression("true"),
                CodeBinaryOperatorType.BooleanAnd,
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeSnippetExpression("base"), "Equals"), new CodeSnippetExpression("obj")));

            if (classPom.Attributes.Count > 0)
            {
                List<CodeSnippetStatement> snipets = new List<CodeSnippetStatement>()
             { new CodeSnippetStatement("\t\t\t\t"+classPom.Name+ " x = "+ "("+classPom.Name+")"+"obj;"),new CodeSnippetStatement("\t\t\t\treturn (" )};

                int k = 1;
                CodeSnippetStatement ste = new CodeSnippetStatement();
                foreach (var item in classPom.Attributes)
                {
                    if (classPom.Attributes.Count == 1)
                    {
                        if (item.MeasurementType.Contains("List<"))
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(CompareHelper.CompareLists(x." + item.Name + " == " + "this." + item.Name + ",true" + "));"));
                        }
                        else
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(x." + item.Name + " == " + "this." + item.Name + "));"));
                        }

                        snipets.Add(ste);
                        break;
                    }
                    if (k == classPom.Attributes.Count)
                    {

                        if (item.MeasurementType.Contains("List<"))
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(CompareHelper.CompareLists(x." + item.Name + " == " + "this." + item.Name + ",true" + ")));"));
                        }
                        else
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(x." + item.Name + " == " + "this." + item.Name + "));"));
                        }
                        snipets.Add(ste);
                        break;
                    }
                    if (item.MeasurementType != "")
                    {
                        if (item.MeasurementType.Contains("List<"))
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(CompareHelper.CompareLists(x." + item.Name + " == " + "this." + item.Name + ",true" + ")) &&"));
                        }
                        else
                        {
                            ste = new CodeSnippetStatement(("\t\t\t\t(x." + item.Name + " == " + "this." + item.Name + ") && "));
                        }
                        snipets.Add(ste);
                    }
                    k++;
                }
                CodeStatement[] trueStatements = snipets.ToArray();
                CodeStatement[] falseStatements = { new CodeSnippetStatement("\t\t\t\treturn false;") };
                CodeConditionStatement ifStatement = new CodeConditionStatement(condition, trueStatements, falseStatements);
                method.Statements.Add(ifStatement);
            }
            else
            {
                List<CodeSnippetStatement> snipets = new List<CodeSnippetStatement>()
                    { new CodeSnippetStatement("\t\t\t\treturn true;" )};

                CodeSnippetStatement ste = new CodeSnippetStatement();
                CodeStatement[] trueStatements = snipets.ToArray();
                CodeStatement[] falseStatements = { new CodeSnippetStatement("\t\t\t\treturn false;") };

                CodeConditionStatement ifStatement = new CodeConditionStatement(condition, trueStatements, falseStatements);
                method.Statements.Add(ifStatement);
            }
            file.Members.Add(method);
        }

        private void CreatePropertyForField(CodeTypeDeclaration file, CodeMemberField att, bool get, bool set)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            prop.Type = att.Type;
            prop.Name = StringManipulationManager.CreateHungarianNotation(att.Name);
            if (prop.Name.Equals(file.Name))
            {
                prop.Name = prop.Name + "P";
            }
            if (get)
            {

                prop.HasGet = true;
                prop.GetStatements.Add(new CodeSnippetExpression("return this." + att.Name));
            }
            if (set)
            {
                prop.HasSet = true;
                prop.SetStatements.Add(new CodeSnippetExpression("this." + att.Name + " = value"));
            }
            file.Members.Add(prop);
        }

        private static void CreateHasValueProperty(CodeTypeDeclaration file, CodeMemberField att)
        {
            CodeMemberProperty propHasValue = new CodeMemberProperty();
            propHasValue.Attributes = MemberAttributes.Public;

            propHasValue.Type = new CodeTypeReference(typeof(bool));
            propHasValue.Name = StringManipulationManager.CreateHungarianNotation(att.Name) + "HasValue";
            propHasValue.HasGet = true;
            propHasValue.HasSet = false;
            propHasValue.GetStatements.Add(new CodeSnippetExpression("return this." + att.Name + " != null"));
            file.Members.Add(propHasValue);
        }

        private void CreateIsMandatoryFieldAndProperty(CodeTypeDeclaration file, CodeMemberField att, EAPAttribute attribute)
        {
            CodeMemberField fieldIsMandatory = new CodeMemberField();
            fieldIsMandatory.Attributes = MemberAttributes.Private | MemberAttributes.Const;
            fieldIsMandatory.Type = new CodeTypeReference(typeof(bool));
            fieldIsMandatory.Name = "is" + StringManipulationManager.CreateHungarianNotation(att.Name) + "Mandatory";

            //switch case and set true or false
            if (int.Parse(attribute.Min) == 1)
            {
                fieldIsMandatory.InitExpression = new CodePrimitiveExpression(true);
            }
            else
            {
                fieldIsMandatory.InitExpression = new CodePrimitiveExpression(false);
            }
            file.Members.Add(fieldIsMandatory);

            CodeMemberProperty propIsMandatory = new CodeMemberProperty();
            propIsMandatory.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            propIsMandatory.Type = new CodeTypeReference(typeof(bool));
            propIsMandatory.Name = "Is" + StringManipulationManager.CreateHungarianNotation(att.Name) + "Mandatory";
            propIsMandatory.HasGet = true;
            propIsMandatory.HasSet = false;
            propIsMandatory.GetStatements.Add(new CodeSnippetExpression("return " + fieldIsMandatory.Name));

            file.Members.Add(propIsMandatory);

        }
        private void CreatePropertyForField(CodeTypeDeclaration file, EAPAttribute attribute, CodeMemberField att, bool get, bool set)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            if (attribute.TypeCode != "")
            {
                if (attribute.TypeCode.Equals("Enum") || attribute.TypeCode.Equals("Class"))
                {
                    prop.Type = new CodeTypeReference(attribute.MeasurementType);
                }
            }
            else
            {
                if (attribute.MeasurementType != "")
                {
                    string dataType = StringManipulationManager.GetSystemType(attribute.MeasurementType);
                    prop.Type = new CodeTypeReference(dataType);
                }
            }
            prop.Name = StringManipulationManager.CreateHungarianNotation(att.Name);
            if (prop.Name.Equals(file.Name))
            {
                prop.Name = prop.Name + "P";
            }
            if (get)
            {
                prop.HasGet = true;
                prop.GetStatements.Add(new CodeSnippetExpression("return this." + att.Name));
            }
            if (set)
            {
                prop.HasSet = true;
                prop.SetStatements.Add(new CodeSnippetExpression("this." + att.Name + " = value"));
            }
            file.Members.Add(prop);

        }

        private void CreateParentClass()
        {

            CodeCompileUnit unit = new CodeCompileUnit();
            //namespace
            CodeNamespace nameSpace = new CodeNamespace("Default_Namespace");
            unit.Namespaces.Add(nameSpace);

            //namespace imports
            nameSpace.Imports.Add(new CodeNamespaceImport("System"));

            //class
            CodeTypeDeclaration file = new CodeTypeDeclaration();
            file.IsClass = true;
            file.Name = "IDClass";
            file.TypeAttributes = TypeAttributes.Public;

            //create field
            string fieldName = "_ID";
            CodeMemberField att = new CodeMemberField(typeof(string), fieldName);
            att.Attributes = MemberAttributes.Private;
            att.Comments.Add(new CodeCommentStatement("ID used for reference purposes", true));

            file.Members.Add(att);

            //create property
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            prop.Type = new CodeTypeReference(typeof(string));
            prop.Name = "ID";
            prop.HasGet = true;
            prop.GetStatements.Add(new CodeSnippetExpression("return this." + fieldName));
            prop.HasSet = true;
            prop.SetStatements.Add(new CodeSnippetExpression("this." + fieldName + " = value"));

            file.Members.Add(prop);

            nameSpace.Types.Add(file);

            FilesComplete.Add(unit);
        }


        public void WaitingMethod()
        {
            while (true)
            {
                LogTB = "Waiting for generating files...";
                Thread.Sleep(600);
                LogTB = "Waiting for generating files.";
                Thread.Sleep(600);
                LogTB = "Waiting for generating files..";
                Thread.Sleep(600);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
