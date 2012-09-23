using System;
using AudioLib;
using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.core;
using urakawa.daisy;

namespace Tobi.Common
{
    public class NoAudioContentFoundByFlowDocumentParserEvent : CompositePresentationEvent<TreeNode>
    {
          public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;

          public static bool TreeNodeNeedsAudio(TreeNode node)
          {
              if (node.HasXmlProperty)
              {
                  string localName = node.GetXmlElementLocalName();

                  bool isMath = localName.Equals("math", StringComparison.OrdinalIgnoreCase);
                  bool isSVG = localName.Equals("svg", StringComparison.OrdinalIgnoreCase);

                  if (!isMath
                      && node.GetXmlNamespaceUri() == DiagramContentModelHelper.NS_URL_MATHML)
                  {
                      return false;
                  }

                  if (!isSVG
                      && node.GetXmlNamespaceUri() == DiagramContentModelHelper.NS_URL_SVG)
                  {
                      return false;
                  }

                  if (localName.Equals("img", StringComparison.OrdinalIgnoreCase)
                       || localName.Equals("video", StringComparison.OrdinalIgnoreCase)
                       || isMath
                      || isSVG
                      )
                  {
                      //if (!isMath && !isSVG)
                      //{
                      //    DebugFix.Assert(node.Children.Count == 0);
                      //}
                      return true;
                  }
              }

              if (node.GetTextMedia() != null
                  && !TreeNode.TextOnlyContainsPunctuation(node.GetTextFlattened_()))
              {
                  DebugFix.Assert(node.Children.Count == 0);
                  return true;
              }

              return false;
          }
    
    }
}