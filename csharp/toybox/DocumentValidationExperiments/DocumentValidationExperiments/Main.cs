using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Xml.Schema;

namespace DocumentValidationExperiments
{
    class DocumentValidationExperiments
    {
        static void Main(string[] args)
        {
            //test 1: validate xml files on disk
            //DtdValidateXmlFiles();

            //test 2: schema validate xml files on disk
            //SchemaValidateXmlFiles();

            //test 3: build a tree, make a dom
            Tree tree = TreeLoader.LoadXml(@"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\sampleFormat.xml");
            DomHelper domHelper = new DomHelper();
            domHelper.Init(tree);
            MakeTreeInvalid(tree);

            SchemaValidation validation = new SchemaValidation();
            validation.Validate(domHelper.Document, 
                                @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\sampleFormat.xsd");
            
            foreach (XmlSchemaValidationException ex in validation.Errors)
            {
                TreeNode node = domHelper.findTreeNode((XmlElement)ex.SourceObject);
                
                Console.WriteLine(string.Format("Tree node error! \n\tNode ID = {0}\n\tError={1}", 
                    node.Id, ex.Message));
            }
            Console.WriteLine("Finished");
            Console.ReadKey(); //keep the terminal open

        }

        private static void MakeTreeInvalid(Tree tree)
        {
            //find the first chapter and insert a "q" node as its child
            //assuming the Root has Children
            TreeNode faultyNode = new TreeNode();
            faultyNode.Id = "FAIL";
            faultyNode.XmlProperty = "DoesNotBelongHere";
            tree.Add(faultyNode, tree.Root.Children[0]);

            //the DOMHelper is listening to the children being added and should catch this addition
            //however, the validation hasn't occurred yet
        }

        private static void SchemaValidateXmlFiles()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(@"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\sampleFormat.xml");
            
            SchemaValidation validation = new SchemaValidation();
            validation.Validate(doc, @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\sampleFormat.xsd");
            Console.WriteLine("Program finished.");
            Console.ReadKey(); //prevent the console from closing
        }

        static void DtdValidateXmlFiles()
        {
            List<string> files = new List<string>
                                 {
                                     @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\valid_dtbook.xml",
                                     @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\invalid_dtbook_1.xml",
                                     @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\invalid_dtbook_2.xml",
                                     @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\invalid_dtbook_3.xml",
                                     @"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\invalid_dtbook_4.xml"
                                 };

            DTDValidation xv = new DTDValidation();

            foreach (string file in files)
            {
                Console.WriteLine("==============================");
                xv.Open(file);
                Console.WriteLine(String.Format("Summary: {0} errors.\n\n", xv.GetErrorCount()));
            }
            Console.WriteLine("==============================");
            Console.WriteLine("Program finished.");
            Console.ReadKey(); //prevent the console from closing

        }

       
        static void WriteXml(XmlDocument doc)
        {
            XmlTextWriter writer = new XmlTextWriter
                (@"y:\tobi\trunk\csharp\toybox\DocumentValidationExperiments\sample_files\sampleOutput.xml", Encoding.UTF8);
            doc.WriteTo(writer);
            writer.Close();
        }
    }
}