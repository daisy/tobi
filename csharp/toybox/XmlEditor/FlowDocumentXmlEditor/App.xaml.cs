using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using urakawa;
using urakawa.property.channel;
using urakawa.xuk;
using FlowDocumentXmlEditor.FlowDocumentExtraction;

namespace FlowDocumentXmlEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Project mProject;
        private Uri mProjectUri = null;


        /// <summary>
        /// Parses an command line argument (having form -name:value)
        /// </summary>
        /// <param name="arg">The command line argument to parse</param>
        /// <param name="name">A <see cref="string"/> in which to output the name part of the argument</param>
        /// <param name="val">A <see cref="string"/> in which to return the value part of the argument</param>
        /// <returns>A <see cref="bool"/> indicating if the command line argument was succesfully parsed</returns>
        static bool ParseArgument(string arg, out string name, out string val)
        {
            if (arg.StartsWith("-"))
            {
                string[] parts = arg.Substring(1).Split(new char[] { ':' });
                if (parts.Length > 0)
                {
                    name = parts[0];
                    if (parts.Length == 1)
                    {
                        val = null;
                    }
                    else
                    {
                        val = parts[1];
                        for (int i = 2; i < parts.Length; i++)
                        {
                            val += ":" + parts[i];
                        }
                    }
                    return true;
                }
            }
            name = null;
            val = null;
            return false;
        }

        /// <summary>
        /// Parses an array of command line arguments
        /// </summary>
        /// <param name="args">The command line arguments to parse</param>
        /// <param name="errMsg">A <see cref="string"/> to which any error message is output</param>
        /// <returns>A <see cref="bool"/> indicating if the command line arguments were succesfully parsed</returns>
        private bool ParseCommandLineArguments(string[] args, out string errMsg)
        {
            errMsg = "";
            foreach (string arg in args)
            {
                string name, val;
                if (ParseArgument(arg, out name, out val))
                {
                    switch (name.ToLower())
                    {
                        case "xuk":
                            Uri baseUri = new Uri(Directory.GetCurrentDirectory()+"\\");
                            mProjectUri = new Uri(baseUri, new Uri(val, UriKind.Relative));
                            break;
                        default:
                            errMsg = String.Format("Invalid argument {0}", arg);
                            return false;
                    }
                }
                else
                {
                    errMsg = String.Format("Invalid argument {0}", arg);
                    return false;
                }
            }
            return true;
        }

        public static MainWindow mw2;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string errMsg;
            if (!ParseCommandLineArguments(e.Args, out errMsg))
            {
                MessageBox.Show(
                    String.Format("{0}\nUsage: FlowDocumentXmlEditor [-xuk:<xuk_uri>]", errMsg));
                this.Shutdown(-1);
                return;
            }

            mProject = new Project();

            //MainWindow mw1 = new MainWindow();
            mw2 = new MainWindow(mProject, mProjectUri);

            if (mProjectUri != null)
            {
                OpenXukAction action = new OpenXukAction(mProject, mProjectUri);
                bool wasCancelled;
                ProgressWindow.ExecuteProgressAction(action, out wasCancelled);
                if (wasCancelled)
                {
                    Shutdown(-1);
                    return;
                }
            }

            if (mProject.NumberOfPresentations > 0)
            {
                Presentation pres = mProject.GetPresentation(0);
                TextChannel textCh = null;
                foreach (Channel ch in pres.ChannelsManager.ListOfChannels)
                {
                    if (ch is TextChannel) 
                    {
                        textCh = (TextChannel)ch;
                        break;
                    }
                }
                //UrakawaHtmlFlowDocument doc1 = new UrakawaHtmlFlowDocument(pres.getRootNode(), textCh);
                //mw1.EditedDocument = doc1;

                GenericExtractionVisitor.USE_TEXT_BOX_UIELEMENT = false;
                UrakawaHtmlFlowDocument doc2 = new UrakawaHtmlFlowDocument(pres.RootNode, textCh);
                mw2.EditedDocument = doc2;
            }
            //mw1.Show();
            mw2.Show();

        }
    }
}
