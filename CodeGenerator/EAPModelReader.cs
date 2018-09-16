using System;
using System.Collections.Generic;
using System.IO;
using CodeGenerator.Model;
using System.Collections;
using System.Globalization;
using EA;
using Common;

namespace CodeGenerator
{
    class EAPModelReader : IDisposable
    {
        static public int i = 0;
        static public bool Validate { get; set; }
        protected EA.Repository m_Repository;
        protected EAPModels eapModels;
        public static EA.Package MyModelCopy;
        TextWriter tw;
        String nameModel;


        public EAPModelReader(ref string txt, string name, string output)
        {
            nameModel = output;
            if (Validate)
            {
                i++;
                if (txt != null)
                {
                    txt = System.IO.Path.Combine(txt, string.Format(name + "_{0}.txt", i.ToString()));
                    tw = new StreamWriter(txt);
                }
                else
                {
                    txt = Path.GetFullPath("../" + name + "_" + i.ToString() + ".txt");
                    tw = new StreamWriter(txt);
                }
                tw.WriteLine("VALIDATION LOG!\n\n");
            }
        }

        public void Dispose()
        {
            if (tw != null)
            {
                tw.Dispose();
            }
        }

        public EAPModels CollectClasses(string input, ref string err)
        {
            m_Repository = new EA.Repository();
            try
            {
                m_Repository.OpenFile(input);
            }
            catch
            {
                err = err + "\nIt is not able to open EAP file. Check the instalation of EAP";
                
                return null;
            }

            eapModels = new EAPModels();
            EAPModel model = null;
            EA.Package MyModel = (EA.Package)m_Repository.Models.GetAt(0);
            string packageAlias = MyModel.Packages.Count > 0 ? ((EA.Package)(MyModel.Packages.GetAt(0))).Alias : null;
            if (packageAlias == null || packageAlias == "")
            {
                model = new EAPModel(nameModel);
                eapModels.Models.Add(model);
            }

            for (short iPackage = 0; iPackage < MyModel.Packages.Count; iPackage++)
            {
                EA.Package package = (EA.Package)MyModel.Packages.GetAt(iPackage);
                MyModelCopy = package;
                DoPackage(package, false, model, true);
            }


            for (short iPackage = 0; iPackage < MyModel.Packages.Count; iPackage++)
            {
                EA.Package package = (EA.Package)MyModel.Packages.GetAt(iPackage);
                MyModelCopy = package;
                DoPackage(package, false, model, false);
            }

            FindAttributeEnumCode();
            FixGroupClass();

            m_Repository.CloseFile();

            if (Validate)
            {
                //FindModelCodeIsNotEAP();
                tw.Close();
            }

            return eapModels;

        }

        public void FixGroupClass()
        {
            foreach (EAPModel model in eapModels.Models)
            {
                foreach (KeyValuePair<string, string> nameClass in model.GroupClasNames)
                {
                    if (model.Classes.ContainsKey(nameClass.Key))
                    {
                        EAPClass eapClass = (EAPClass)model.Classes[nameClass.Key];
                        if (model.Classes.ContainsKey(nameClass.Value))
                        {
                            EAPClass eapClassParent = (EAPClass)model.Classes[nameClass.Value];
                            eapClassParent.Attributes.AddRange(eapClass.Attributes);
                            eapClass.Attributes.Clear();
                        }
                        else
                        {
                            if (Validate)
                            {
                                tw.WriteLine("Model Code for class: {0},", eapClass.Code);
                                tw.WriteLine("Name for class: {0},", eapClass.Name);
                                tw.WriteLine("Name for class Parent: {0},", nameClass.Value);

                                tw.WriteLine("Parent class name:" + nameClass.Value + ", does not exist in Eap.");

                                tw.WriteLine("*************************************************************************");
                                tw.WriteLine("\n\n");
                            }

                        }
                    }
                }
            }
        }

        public void FindAttributeEnumCode()
        {
            foreach (EAPModel model in eapModels.Models)
            {
                foreach (DictionaryEntry l_Entry in model.Classes)
                {
                    EAPClass eapClass = (EAPClass)l_Entry.Value;

                    for (int i = 0; i < eapClass.Attributes.Count; i++)
                    {
                        if (eapClass.Attributes[i].TypeCode != null)
                        {
                            if (model.Enums.ContainsKey(eapClass.Attributes[i].TypeCode))
                            {
                                eapClass.Attributes[i].TypeCode = model.Enums[eapClass.Attributes[i].TypeCode].Code;
                            }
                            else
                            {
                                if (Validate)
                                {
                                    tw.WriteLine("Model Code for class: {0},", eapClass.Code);
                                    tw.WriteLine("Name for class: {0},", eapClass.Name);
                                    tw.WriteLine("Name for attribute: {0},", eapClass.Attributes[i].Name);
                                    tw.WriteLine("Model Code for attribute: {0},", eapClass.Attributes[i].Code);
                                    tw.WriteLine("enum name for attribute: {0},\n", eapClass.Attributes[i].TypeCode);

                                    tw.WriteLine(eapClass.Attributes[i].TypeCode + " enum Name does not exist in Eap.");

                                    tw.WriteLine("*************************************************************************");
                                    tw.WriteLine("\n\n");
                                }
                            }
                        }
                    }

                }
            }
        }


        public void DoPackage(EA.Package MyPackage, bool domain, EAPModel model, bool flag)
        {

            bool parse = true;
            try
            {
                parse = (int)Convert.ToDouble(MyPackage.Version) != 1 ? false : true;
            }
            catch
            {
                parse = false;
            }

            if (MyPackage.Name == "Domain")
                domain = true;

            //string packageAlias = MyPackage.Alias;
            //if (packageAlias != null && packageAlias != "")
            //{
            //    string nameXml = System.IO.Path.GetFileNameWithoutExtension(nameModel);
            //    string path = nameModel.Replace(nameXml, packageAlias);
            //    model = new EAPModel(path);
            //    eapModels.Models.Add(model);
            //}
            //else if (model == null)
            //    return;

            MyModelCopy = MyPackage;
            if (flag)
            {
                for (short i = 0; i < MyPackage.Elements.Count; i++)
                {
                    EA.Element MyElem = (EA.Element)MyPackage.Elements.GetAt(i);

                    if (MyElem.MetaType.Equals("Enumeration", StringComparison.InvariantCultureIgnoreCase))
                    {
                        CollectEnums(MyElem, parse, model);
                    }

                    if (MyElem.Type.Equals("Class", StringComparison.InvariantCultureIgnoreCase))
                    {
                        CollectClass(MyElem, parse, model, flag);
                    }
                }
            }
            else
            {
                for (short i = 0; i < MyPackage.Elements.Count; i++)
                {
                    EA.Element MyElem = (EA.Element)MyPackage.Elements.GetAt(i);
                    if (MyElem.Type.Equals("Class", StringComparison.InvariantCultureIgnoreCase))
                    {
                        CollectClass(MyElem, parse, model, flag);
                    }
                }
            }

            if (!parse)
            {
                if (Validate)
                {
                    tw.WriteLine("Package name:" + MyPackage.Name + ",  - not processed.");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }

            for (short iPackage = 0; iPackage < MyPackage.Packages.Count; iPackage++)
            {
                DoPackage((EA.Package)MyPackage.Packages.GetAt(iPackage), domain, model, flag);
            }
        }

        public void CollectEnums(EA.Element elem, bool parse, EAPModel model)
        {
            if (!model.Enums.ContainsKey(elem.Name))
            {
                EAPEnumeration en = new EAPEnumeration();

                en.Name = elem.Name;
                en.Code = elem.Alias;
                en.Description = elem.Notes;
                en.Bitfield = elem.Stereotype.Contains("bitfield");
                en.Parse = parse;

                if (en.Description.Split(':').Length > 1)
                {
                    en.Title = en.Description.Split(':')[0];
                    en.Description = en.Description.Remove(0, en.Title.Length + 1).Trim();
                }
                else
                {
                    en.Title = "";
                    en.Description = en.Description.Trim();
                }
                en.Title = en.Title.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();
                en.Description = en.Description.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();

                

                foreach (EA.Attribute l_Attr in elem.Attributes)
                {
                    CollectEnumValue(l_Attr, en);
                }

                model.Enums.Add(en.Name, en);

                if (Validate && !en.Parse && en.EnumValue.Count != 0)
                {
                    tw.WriteLine("Model Code for enum: {0},", en.Code);
                    tw.WriteLine("Name for enum: {0},\n", en.Name);
                    tw.WriteLine("Enum name:" + en.Name + ", was not being processed but it has enum value.");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }
            else
            {
                if (Validate)
                {
                    tw.WriteLine("Enum name:" + elem.Name + ", already exists in EAP.");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }

        }
        public string ReturnValue(MetaEntity obj, EA.Attribute l_Attr, EAPEnumeration en = null)
        {
            if (l_Attr.Default != null && (l_Attr.Default != ""))
            {
                if (!l_Attr.Default.StartsWith("0x", true, CultureInfo.InvariantCulture))
                    return l_Attr.Default;
                else
                {
                    string helpString = l_Attr.Default.Remove(0, 2).Trim();

                    try
                    {
                        long val = Convert.ToInt64(helpString, 16);
                        return val.ToString();
                    }
                    catch
                    {
                        if (Validate)
                        {
                            if (en != null)
                            {
                                tw.WriteLine("Model Code for enum: {0},", en.Code);
                                tw.WriteLine("Name for enum: {0},", en.Name);
                            }
                            else
                                tw.WriteLine("Model Code: {0},", obj.Code);

                            tw.WriteLine("Name: {0},", obj.Name);
                            tw.WriteLine("Value Default {0},\n", l_Attr.Default);
                            tw.WriteLine(l_Attr.Default + " value can not be converted  in number.");

                            tw.WriteLine("*************************************************************************");
                            tw.WriteLine("\n\n");
                        }
                    }
                }
            }
            return null;
        }

        public void CollectEnumValue(EA.Attribute l_Attr, EAPEnumeration en)
        {
            EAPEnumValue enValue = new EAPEnumValue();

            enValue.Name = l_Attr.Name;
            enValue.Description = l_Attr.Notes;

            enValue.Value = ReturnValue(enValue, l_Attr, en);

            if (enValue.Description.Split(':').Length > 1)
            {
                enValue.Title = enValue.Description.Split(':')[0];
                enValue.Description = enValue.Description.Remove(0, enValue.Title.Length + 1).Trim();
            }
            else
            {
                enValue.Title = "";
                enValue.Description = enValue.Description.Trim();
            }
            enValue.Title = enValue.Title.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();
            enValue.Description = enValue.Description.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();

            //enValue.Value = (long)Convert.ChangeType(Enum.Parse(proba, enValue.Name), typeof(long));
            if (!(en.AddEnumValue(enValue)))
            {
                if (Validate)
                {
                    tw.WriteLine("Enum value name:" + l_Attr.Name + ",  already exists in Enum name: " + en.Name + ". (Look at EAP)");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }

          
        }

        public void FindType(EAPClass l_Class, EAPAttribute l_MyAttr, EA.Attribute l_Attr)
        {
            //PropertyType type = 0;
            if ((l_MyAttr.Code != null && l_MyAttr.Code != "") || true)
            {
                if (Enum.IsDefined(typeof(MeasurementType), l_Attr.Type.ToUpper()))
                {
                    l_MyAttr.MeasurementType = l_Attr.Type;

                }
                else
                {
                    List<string> typeStr = new List<string>() { "bool", "int", "long", "string", "short", "double", "float",
                                "byte","modelcode", "lid", "gid"};
                    if (l_Attr.Type.Split('<').Length > 1)
                    {
                        string helpString = l_Attr.Type.Remove(0, (l_Attr.Type.IndexOf('<') + 1));
                        // l_MyAttr.MeasurementType = helpString.Remove(helpString.IndexOf('>'));
                        l_MyAttr.MeasurementType = l_Attr.Type;
                        if (Enum.IsDefined(typeof(MeasurementType), helpString.Remove(helpString.IndexOf('>'))))
                        {
                              l_MyAttr.MeasurementType = helpString.Remove(helpString.IndexOf('>'));
                          
                        }
                        else
                        {
                            l_MyAttr.MeasurementType = l_Attr.Type;
                        }
                    }
                    else
                    {
                      
                        if (!typeStr.Contains(l_Attr.Type.ToLower()))
                        {
                            l_MyAttr.MeasurementType = l_Attr.Type;
                        }
                        else if (typeStr.Contains(l_Attr.Type.ToLower()))
                        {
                            //GenerateUIControlsBasedOnPropType(l_Attr.Type.ToLower(), l_MyAttr, false);
                        }
                    }
                }
            }
        }
        public void CollectClass(EA.Element p_Elem, bool parse, EAPModel model, bool flag)
        {
            if (p_Elem.Name == "LID" || p_Elem.Name == "GID") return;

            //     if (!model.Classes.ContainsKey(p_Elem.Name))
            //  {
            EAPClass l_Class = new EAPClass();
            EAPClass l_ClassParent = new EAPClass();
            l_Class.Name = p_Elem.Name;
            l_Class.Abstract = p_Elem.Abstract == "0" ? "false" : "true"; // Leaf
            l_Class.Parse = parse;



            //group
            bool group = p_Elem.Tag == "Contained";
            bool existClass = false;

            //Description i title
            l_Class.Description = p_Elem.Notes;
            if (l_Class.Description.Split(':').Length > 1)
            {
                l_Class.Title = l_Class.Description.Split(':')[0];
                l_Class.Description = l_Class.Description.Remove(0, l_Class.Title.Length + 1).Trim();
            }
            else
            {
                l_Class.Title = "";
                l_Class.Description = l_Class.Description.Trim();
            }

            l_Class.Title = l_Class.Title.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();
            l_Class.Description = l_Class.Description.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();

            l_Class.Code = p_Elem.Alias; // ModelCode


            foreach (EA.Element l_Parent in p_Elem.BaseClasses)
            {
                if (l_Parent.Type == "Class")
                {
                    l_Class.Parent = l_Parent.Name;

                    if (group)
                    {
                        existClass = model.Classes.ContainsKey(l_Parent.Name);

                        if (existClass)
                            l_ClassParent = (EAPClass)model.Classes[l_Parent.Name];
                        else
                        {
                            model.GroupClasNames.Add(p_Elem.Name, l_Parent.Name);
                        }
                    }
                }
            }

            if (!group || !existClass)
            {
                //collect attributes
                if (flag == false)
                {
                    AddAttributesInClass(model, p_Elem,(EAPClass) model.Classes[l_Class.Name], group);
                }

            }
            else
            {
                if (flag == false)
                {
                    AddAttributesInClass(model, p_Elem, l_ClassParent, group, l_Class);
                }
            }
            if (!model.Classes.ContainsKey(p_Elem.Name))
                model.Classes.Add(l_Class.Name, l_Class);


            //import inner classes attributes as atributes
            if (!group)
            {
                foreach (EA.Element el_Class in p_Elem.Elements)
                {
                    CollectClass(el_Class, parse, model, flag);
                }
            }
        }
    
        public void AddAttributesInClass(EAPModel model, EA.Element p_Elem, EAPClass l_Class, bool group, EAPClass l_ClassGroup = null)
        {
            if (p_Elem.Attributes.Count != 0)
            {
                foreach (EA.Attribute l_Attr in p_Elem.Attributes)
                {
                    CollectAttribute(model, l_Attr, l_Class, group, l_ClassGroup);
                }
            }
            if (p_Elem.Connectors.Count != 0)
            {
                foreach (EA.Connector l_Attr in p_Elem.Connectors)
                {
                    if (l_Attr.Type.Equals("Aggregation") || l_Attr.Type.Equals("Composition") || l_Attr.Type.Equals("Association"))
                        CollenctConectors(model, l_Attr, l_Class, group, p_Elem);
                }

            }


        }

        private void CollenctConectors(EAPModel model, Connector l_Attr, EAPClass l_Class, bool group, EA.Element l_class)
        {
            EAPAttribute l_MyAttr = new EAPAttribute();
            l_MyAttr.Description = l_Attr.Notes;


            if (l_MyAttr.Description.Split(':').Length > 1)
            {
                l_MyAttr.Title = l_MyAttr.Description.Split(':')[0];
                l_MyAttr.Description = l_MyAttr.Description.Remove(0, l_MyAttr.Title.Length + 1);
            }
            else
            {
                l_MyAttr.Title = "";
                l_MyAttr.Description = l_MyAttr.Description.Trim();
            }

            l_MyAttr.Title = l_MyAttr.Title.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();
            l_MyAttr.Description = l_MyAttr.Description.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();

            l_MyAttr.Code = l_Attr.Alias;
            bool pom = false;
            if (l_class.ElementID == l_Attr.ClientID)
            {
                l_MyAttr.Name = l_Attr.SupplierEnd.Role;
                l_MyAttr.Code = l_Attr.SupplierEnd.Alias;
                if (StringManipulationManager.GetMaxCardinality(l_Attr.SupplierEnd.Cardinality).Equals("*"))
                {
                    l_MyAttr.Max = "*";
                    string min = l_Attr.SupplierEnd.Cardinality.Substring(0, 1);
                    l_MyAttr.Min = min;
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.ClientEnd.Role, l_Attr.ClientEnd.Alias);
                }
                else if (StringManipulationManager.GetMaxCardinality(l_Attr.SupplierEnd.Cardinality).Equals("1"))
                {
                    l_MyAttr.Max = "1";
                    l_MyAttr.Min = "1";
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.ClientEnd.Role, l_Attr.ClientEnd.Alias);
                }
                else if (StringManipulationManager.GetMaxCardinality(l_Attr.ClientEnd.Cardinality).Equals("2"))
                {
                    l_MyAttr.Max = "2";
                    l_MyAttr.Min = "0";
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.ClientEnd.Role, l_Attr.ClientEnd.Alias);
                }
                pom = true;
            }
            else
            {
                l_MyAttr.Name = l_Attr.ClientEnd.Role;
                l_MyAttr.Code = l_Attr.ClientEnd.Alias;
                if (StringManipulationManager.GetMaxCardinality(l_Attr.ClientEnd.Cardinality).Equals("*"))
                {
                    l_MyAttr.Max = "*";
                    string min = l_Attr.ClientEnd.Cardinality.Substring(0, 1);
                    l_MyAttr.Min = min;
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.SupplierEnd.Role, l_Attr.SupplierEnd.Alias);
                }
                else if (StringManipulationManager.GetMaxCardinality(l_Attr.ClientEnd.Cardinality).Equals("1"))
                {
                    l_MyAttr.Max = "1";
                    l_MyAttr.Min = "1";
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.SupplierEnd.Role, l_Attr.SupplierEnd.Alias);
                }
                else if (StringManipulationManager.GetMaxCardinality(l_Attr.ClientEnd.Cardinality).Equals("2"))
                {
                    l_MyAttr.Max = "2";
                    l_MyAttr.Min = "0";
                    // pair of attributes on same connector for Add and Remove Ref
                    l_MyAttr.onSameConnectorPairAtt = new EAPAttribute(l_Attr.SupplierEnd.Role, l_Attr.SupplierEnd.Alias);
                }
            }

            if (l_MyAttr.Max == "*")
            {
                if (pom)
                    l_MyAttr.MeasurementType = "List<" + FindClassById(l_Attr.SupplierID, MyModelCopy) + ">";
                else
                    l_MyAttr.MeasurementType = "List<" + FindClassById(l_Attr.ClientID, MyModelCopy) + ">";

                l_MyAttr.IsListOfReferences = true;  

            }
            else if (l_MyAttr.Max == "1")
            {
                l_MyAttr.MeasurementType = "long";
                l_MyAttr.IsReference = true;  
            }


            //  FindType(l_Class, l_MyAttr, l_Attr);

            //l_MyAttr.Aggregated = GetAggregated(l_Attr, l_MyAttr);
            //   l_MyAttr.Searchable = GetSearchable(l_Attr, l_MyAttr);
            //  l_MyAttr.Cardinality = GetCardinality(l_Attr, l_MyAttr) == 0 ? null : GetCardinality(l_Attr, l_MyAttr).ToString();

            if (!(l_Class.AddAttribute(l_MyAttr)))
            {
                if (Validate)
                {
                    tw.WriteLine("Attribute Model Code:" + l_Attr.Alias + ",  already exists in Class name: " + l_Class.Name + ". (Look at EAP)");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }
        }

        public void CollectAttribute(EAPModel model, EA.Attribute l_Attr, EAPClass l_Class, bool group, EAPClass l_ClassGroup = null)
        {
            EAPAttribute l_MyAttr = new EAPAttribute();
            l_MyAttr.Description = l_Attr.Notes;
            l_MyAttr.Settable = !l_Attr.IsConst;
            l_MyAttr.Max = l_Attr.UpperBound;
            l_MyAttr.Min = l_Attr.LowerBound;
            l_MyAttr.Index = l_Attr.Pos;
            l_MyAttr.Default = ReturnValue(l_MyAttr, l_Attr);
            l_MyAttr.Visible = !l_Attr.IsStatic;

            if (model.Enums.ContainsKey(l_Attr.Type))
            {
                l_MyAttr.TypeCode = "Enum";
            }
            if (model.Classes.ContainsKey(l_Attr.Type))
            {
                l_MyAttr.TypeCode = "Class";
            }

            if (group)
            {
                if (l_ClassGroup == null)
                    l_MyAttr.Group = l_Class.Code;
                else
                    l_MyAttr.Group = l_ClassGroup.Code;
            }

            if (l_MyAttr.Description.Split(':').Length > 1)
            {
                l_MyAttr.Title = l_MyAttr.Description.Split(':')[0];
                l_MyAttr.Description = l_MyAttr.Description.Remove(0, l_MyAttr.Title.Length + 1);
            }
            else
            {
                l_MyAttr.Title = "";
                l_MyAttr.Description = l_MyAttr.Description.Trim();
            }

            l_MyAttr.Title = l_MyAttr.Title.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();
            l_MyAttr.Description = l_MyAttr.Description.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "").Trim();

            l_MyAttr.Code = l_Attr.Style;
            l_MyAttr.Name = l_Attr.Name;

            FindType(l_Class, l_MyAttr, l_Attr);

            l_MyAttr.Aggregated = GetAggregated(l_Attr, l_MyAttr);
            l_MyAttr.Searchable = GetSearchable(l_Attr, l_MyAttr);
            l_MyAttr.Cardinality = GetCardinality(l_Attr, l_MyAttr) == 0 ? null : GetCardinality(l_Attr, l_MyAttr).ToString();


            if (!(l_Class.AddAttribute(l_MyAttr)))
            {
                if (Validate)
                {
                    tw.WriteLine("Attribute Model Code:" + l_Attr.Style + ",  already exists in Class name: " + l_Class.Name + ". (Look at EAP)");

                    tw.WriteLine("*************************************************************************");
                    tw.WriteLine("\n\n");
                }
            }
        }

        public bool GetAggregated(EA.Attribute l_Attr, EAPAttribute l_MyAttr)
        {
            bool aggregated = false;

            foreach (EA.AttributeConstraint str in l_Attr.Constraints)
            {
                if (str.Name.ToLower() == "aggregated")
                {
                    try
                    {
                        if (str.Notes.Trim() == "0")
                            return false;
                        else if (str.Notes.Trim() == "1")
                            return true;

                        aggregated = Convert.ToBoolean(str.Notes.Trim());
                        return aggregated;
                    }
                    catch
                    {
                        if (Validate)
                        {
                            tw.WriteLine("Model Code: {0},", l_MyAttr.Code);
                            tw.WriteLine("Name: {0},", l_MyAttr.Name);
                            tw.WriteLine("Aggregated: {0},\n", str.Notes);
                            tw.WriteLine(str + "aggregated value can not be converted  in bool.");

                            tw.WriteLine("*************************************************************************");
                            tw.WriteLine("\n\n");
                        }
                    }
                }
            }
            return aggregated;
        }

        public int GetCardinality(EA.Attribute l_Attr, EAPAttribute l_MyAttr)
        {
            int cardinality = 0;

            foreach (EA.AttributeConstraint str in l_Attr.Constraints)
            {
                if (str.Name.ToLower() == "cardinality")
                {
                    try
                    {
                        cardinality = Convert.ToInt32(str.Notes.Trim());
                        return cardinality;
                    }
                    catch
                    {
                        if (Validate)
                        {
                            tw.WriteLine("Model Code: {0},", l_MyAttr.Code);
                            tw.WriteLine("Name: {0},", l_MyAttr.Name);
                            tw.WriteLine("Cardinality: {0},\n", str.Notes);
                            tw.WriteLine(str + "cardinality value can not be converted  in int.");

                            tw.WriteLine("*************************************************************************");
                            tw.WriteLine("\n\n");
                        }
                    }
                }
            }
            return cardinality;
        }

        public bool GetSearchable(EA.Attribute l_Attr, EAPAttribute l_MyAttr)
        {
            bool searchable = false;

            foreach (EA.AttributeConstraint str in l_Attr.Constraints)
            {
                if (str.Name.ToLower() == "searchable")
                {
                    try
                    {
                        if (str.Notes.Trim() == "0")
                            return false;
                        else if (str.Notes.Trim() == "1")
                            return true;

                        searchable = Convert.ToBoolean(str.Notes.Trim());
                        return searchable;
                    }
                    catch
                    {
                        if (Validate)
                        {
                            tw.WriteLine("Model Code: {0},", l_MyAttr.Code);
                            tw.WriteLine("Name: {0},", l_MyAttr.Name);
                            tw.WriteLine("Searchable: {0},\n", str.Notes);
                            tw.WriteLine(str.Notes + "searchable value can not be converted  in bool.");

                            tw.WriteLine("*************************************************************************");
                            tw.WriteLine("\n\n");
                        }
                    }
                }
            }
            return searchable;
        }
        public string FindClassById(int id, EA.Package MyPackage)
        {
            for (short i = 0; i < MyPackage.Elements.Count; i++)
            {
                EA.Element MyElem = (EA.Element)MyPackage.Elements.GetAt(i);
                if (MyElem.Type.Equals("Class", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (MyElem.ElementID == id)
                    {
                        return MyElem.Name;
                    }
                }
            }
            return null;
        }

    }
}

