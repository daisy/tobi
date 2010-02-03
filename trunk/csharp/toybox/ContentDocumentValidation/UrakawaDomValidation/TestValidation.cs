using System;
using System.IO;
using System.Text.RegularExpressions;
using urakawa;
using urakawa.core;
using urakawa.xuk;

namespace UrakawaDomValidation
{
    public class TestValidation
    {
        public static void Main(string[] args)
        {
            string dtd = @"..\..\..\dtbook-2005-3.dtd";

            //valid files
            string simplebook = @"..\..\..\simplebook.xuk";
            string paintersbook = @"..\..\..\greatpainters.xuk";

            //invalid files
            string simplebook_invalid_doublefrontmatter = @"..\..\..\simplebook-invalid-doublefrontmatter.xuk";
            string simplebook_invalid_unrecognized_element = @"..\..\..\simplebook-invalid-element.xuk";
            string simplebook_invalid_badnesting = @"..\..\..\simplebook-invalid-badnesting.xuk";

            //the dtd cache location (hardcoded in this example.  
            //eventually, we'll need a table of dtd to cached version)
            string dtdcache = @"..\..\..\dtd.cache";

            bool writeToLog = false;
            bool reportTraces = false;

            FileStream ostrm = null;
            StreamWriter writer = null;
            TextWriter oldOut = null;

            if (writeToLog)
            {
                oldOut = Console.Out;
                ostrm = new FileStream(@"..\..\..\log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
                Console.SetOut(writer);
            }

            UrakawaDomValidation validator = new UrakawaDomValidation();
            bool readFromCache = false;

            if (readFromCache)
            {
                validator.UseCachedDtd(new StreamReader(Path.GetFullPath(dtdcache)));
            }
            else
            {
                validator.UseDtd(new StreamReader(Path.GetFullPath(dtd)));
                validator.SaveCachedDtd(new StreamWriter(Path.GetFullPath(dtdcache)));
            }

            Project project = LoadXuk(simplebook_invalid_doublefrontmatter);
            TreeNode root = project.Presentations.Get(0).RootNode;
            bool res = validator.Validate(root);

            Console.WriteLine(res ? "VALID!!" : "INVALID!!");

            foreach (UrakawaDomValidationReportItem item in validator.ValidationReportItems)
            {
                if (item.ItemType == UrakawaDomValidationReportItem.ReportItemType.Error ||
                    (item.ItemType == UrakawaDomValidationReportItem.ReportItemType.Trace &&
                    reportTraces == true))
                {
                    string type = item.ItemType == UrakawaDomValidationReportItem.ReportItemType.Trace
                                      ? "TRACE"
                                      : "ERROR";
                    Console.WriteLine(string.Format("* {0}", type));
                    Console.WriteLine(string.Format("Node name: {0}", item.Node.GetXmlElementQName().LocalName));
                    Console.WriteLine(string.Format("Reg ex: {0}", validator.GetRegex(item.Node)));
                    Console.WriteLine(item.Message);
                    Console.WriteLine("\n");
                }
            }
            if (writeToLog)
            {
                Console.SetOut(oldOut);
                writer.Close();
                ostrm.Close();
                Console.WriteLine(res ? "VALID!!" : "INVALID!!");
            }

            TestRegex(validator);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        public static Project LoadXuk(string file)
        {
            string fullpath = Path.GetFullPath(file);
            Project project = new Project();
            var uri = new Uri(fullpath);
            var action = new OpenXukAction(project, uri);
            action.Execute();
            return project;
        }
        public static void TestRegex(UrakawaDomValidation validator)
        {
            Console.WriteLine("Enter an element name:");
            string elm = Console.ReadLine();

            string regexStr = validator.GetRegex(elm);
            if (string.IsNullOrEmpty(regexStr))
            {
                Console.WriteLine("Element definition not found!");
                return;
            }

            
            Console.WriteLine("Enter the children for this element, separated by a comma:");
            string children = Console.ReadLine();
            children = children.Replace(", ", "#");
            children = children.Replace(",", "#");
            if (!children.EndsWith("#")) children += "#";
            Console.WriteLine(children);
            Regex regex = new Regex(regexStr);

            Match match = regex.Match(children);
            foreach (Group g in match.Groups)
            {
                Console.WriteLine(string.Format("Group: {0}", g.ToString()));
            }
            if (match.Success == true && match.ToString() == children)
            {
                Console.WriteLine("SUCCESS!");
                return;
            }

            if (match.Success == false)
            {
                Console.WriteLine("No match!");
                Console.WriteLine(regex.ToString());
                Console.WriteLine(string.Format("Assume that {0} are not allowed children.", children));
                return;
            }

            Console.WriteLine(string.Format("Match: {0}", match.ToString()));
            
            if (match.ToString() != children)
            {
                string[] childrenArr = children.Split('#');
                foreach (string child in childrenArr)
                {
                    if (match.ToString().IndexOf(child) == -1)
                    {
                        Console.WriteLine(string.Format("{0} not found in match", child));
                    }
                    if (regex.ToString().IndexOf(child) == -1)
                    {
                        Console.WriteLine(string.Format("{0} not found in regex", child));
                    }
                }                
            }
        }
    }
}
