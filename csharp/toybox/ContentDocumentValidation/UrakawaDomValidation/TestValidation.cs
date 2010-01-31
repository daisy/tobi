using System;
using System.IO;
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
            //it has two frontmatter items whereas only zero or one is allowed.
            string simplebook_invalid_doublefrontmatter = @"..\..\..\simplebook-invalid-doublefrontmatter.xuk";
            //these both register as invalid, and for the right reasons
            string simplebook_invalid_unrecognized_element = @"..\..\..\simplebook-invalid-element.xuk";
            string simplebook_invalid_badnesting = @"..\..\..\simplebook-invalid-badnesting.xuk";

            //the dtd cache location (hardcoded in this example.  
            //eventually, we'll need a table of dtd to cached version)
            string dtdcache = @"..\..\..\dtd.cache";

            bool writeToLog = true;

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

            Project project = LoadXuk(paintersbook);
            TreeNode root = project.Presentations.Get(0).RootNode;
            bool res = validator.Validate(root);

            Console.WriteLine(res ? "VALID!!" : "INVALID!!");

            foreach (UrakawaDomValidationReportItem item in validator.ValidationReportItems)
            {
                string type = item.ItemType == UrakawaDomValidationReportItem.ReportItemType.Trace ? "TRACE" : "ERROR";
                Console.WriteLine(string.Format("* {0}", type));
                Console.WriteLine(string.Format("Node name: {0}", item.Node.GetXmlElementQName().LocalName));
                Console.WriteLine(string.Format("Reg ex: {0}", validator.GetRegex(item.Node)));
                Console.WriteLine(item.Message);
                Console.WriteLine("\n");
            }
            if (writeToLog)
            {
                Console.SetOut(oldOut);
                writer.Close();
                ostrm.Close();
                Console.WriteLine(res ? "VALID!!" : "INVALID!!");
            }
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
    }
}
