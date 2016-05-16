using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ltkgo_generator
{
    class Program
    {
        static string mapLlrpTypeToGoType(string llrpType)
        {
            switch (llrpType)
            {
                case "Custom":
                    return "interface {}";

                case "LLRPStatus":
                    return "LLRPStatus";

                case "GeneralDeviceCapabilities":
                    return "GeneralDeviceCapabilities";

                case "LLRPCapabilities":
                    return "LLRPCapabilities";

                case "RegulatoryCapabilities":
                    return "RegulatoryCapabilities";

                case "ROSpec":
                    return "ROSpec";

                case "AccessSpec":
                    return "AccessSpec";

                case "TagReportData":
                    return "TagReportData";

                case "ClientRequestResponse":
                    return "ClientRequestResponse";

                case "Identification":
                    return "Identification";

                case "u1":
                    return "bool";

                case "u8":
                    return "byte";

                case "u16":
                    return "uint16";

                case "u32":
                    return "uint32";

                case "u64":
                    return "uint64";

                case "bytesToEnd":

                case "s8":
                case "s16":

                case "u2":
                case "u96":

                case "u1v":
                case "u8v":
                case "u16v":
                case "u32v":

                case "utf8v":
                    return "unknownType";

                default:
                    Console.WriteLine("Unknown LLRP type: " + llrpType);
                    return String.Empty;
            }
        }

        static void Main(string[] args)
        {
            var r = XmlReader.Create("llrp-1x0-def.xml");
            while (r.NodeType != XmlNodeType.Element)
                r.Read();
            var root = XElement.Load(r);

            XNamespace ld = "http://www.llrp.org/ltk/schema/core/encoding/binary/1.0";
            var c1 = from el in root.Elements(ld + "parameterDefinition")
                     select el;

            foreach (var el in c1)
            {
                String name = el.Attribute("name").Value;

                using (var fs = File.Create(name + ".go"))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine("package ltkgo");
                    w.WriteLine();
                    w.WriteLine(String.Format("type {0} struct {{", name));

                    foreach (var field in el.Elements(ld + "field"))
                    {
                        String fieldName = field.Attribute("name").Value,
                            type = mapLlrpTypeToGoType(field.Attribute("type").Value);

                        w.WriteLine("\t{0} {1} `xml:\"{2}\"`",
                            fieldName,
                            type,
                            fieldName);
                    }

                    w.WriteLine("}");
                }

                using (var fs = File.Create(name + "_test.go"))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine("package ltkgo_test");
                    w.WriteLine();
                }
            }

            c1 = from el in root.Elements(ld + "messageDefinition")
                 select el;

            foreach (var el in c1)
            {
                String name = el.Attribute("name").Value;

                using (var fs = File.Create(name + ".go"))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine("package ltkgo");
                    w.WriteLine();
                    w.WriteLine(String.Format("type {0} struct {{", name));

                    foreach (var field in el.Elements(ld + "field"))
                    {
                        String fieldName = field.Attribute("name").Value,
                            fieldType = mapLlrpTypeToGoType(field.Attribute("type").Value);

                        w.WriteLine("\t{0} {1} `xml:\"{2}\"`",
                            fieldName,
                            fieldType,
                            fieldName);
                    }

                    foreach (var param in el.Elements(ld + "parameter"))
                    {
                        String paramName = param.Attribute("type").Value,
                            paramType = mapLlrpTypeToGoType(param.Attribute("type").Value),
                            paramRepeat = param.Attribute("repeat").Value;

                        var isArray = paramRepeat == "0-N";
                        w.WriteLine("\t{0} {1} `xml:\"{2}\"`",
                            paramName,
                            isArray ? "[]" + paramType : paramType,
                            paramName);
                    }

                    w.WriteLine("}");

                    var hasResponse = el.Attributes("responseType").Any();
                    if (hasResponse)
                    {
                        var response = el.Attribute("responseType").Value;

                        w.WriteLine();
                        w.WriteLine(String.Format("func (s {0}) GetResponseType() interface {{}} {{", name));
                        w.WriteLine(String.Format("\treturn reflect.TypeOf((*{0})(nil)).Elem()", response));
                        w.WriteLine("}");
                    }
                }

                using (var fs = File.Create(name + "_test.go"))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine("package ltkgo_test");
                    w.WriteLine();
                }
            }

            c1 = from el in root.Elements(ld + "enumerationDefinition")
                 select el;

            foreach (var el in c1)
            {
                String name = el.Attribute("name").Value;

                using (var fs = File.Create(name + ".go"))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine("package ltkgo");
                    w.WriteLine();
                    w.WriteLine(String.Format("type {0} int", name));
                    w.WriteLine();
                    w.WriteLine("const (");

                    foreach (var entry in el.Elements(ld + "entry"))
                    {
                        String entryName = entry.Attribute("name").Value,
                            entryValue = entry.Attribute("value").Value;

                        w.WriteLine(String.Format("\t{0} {1} = {2}",
                            String.Format("{0}_{1}", name, entryName),
                            name,
                            entryValue));
                    }

                    w.WriteLine(")");
                }
            }
        }
    }
}
