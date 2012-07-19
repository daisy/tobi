using System;
using Org.System.Xml.Sax.Helpers;

/*** License **********************************************************
*
* Copyright (c) 2005, Jeff Rafter
*
* This file is part of the AElfred C# library. 
*
* AElfred is free for both commercial and non-commercial use and
* redistribution, provided that the following copyrights and disclaimers 
* are retained intact.  You are free to modify AElfred for your own use and
* to redistribute AElfred with your modifications, provided that the
* modifications are clearly documented.
*
* This program is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of
* merchantability or fitness for a particular purpose.  Please use it AT
* YOUR OWN RISK.
*/

namespace AElfred
{

  // Interface for push style content model parsing

  /// <summary>
  /// Handler to allow the parser to pass information about each content model. This 
  /// information should be reported before any ElementDecl callbacks. Additionally,
  /// The information is designed to allow the correct nesting of entity boundaries.
  /// </summary>
  public interface IContentModelHandler 
  {
    /// <summary>
    /// The start of a content model. For every content model this event must be reported.
    /// </summary>
    /// <param name="name">The name of the element that is associated with this content model.</param>    
    void StartContentModel(string name);

    /// <summary>
    /// The end of a content model. For every legal content model this event must be reported.
    /// If an error occurs before the end of the content model is reached, this event is not called.
    /// </summary>
    void EndContentModel();    

    /// <summary>
    /// The content model is "EMPTY".
    /// </summary>
    void Empty();

    /// <summary>
    /// The content model is "ANY".
    /// </summary>
    void Any();

    /// <summary>
    /// The start of a group (i.e., a "(" was encountered).
    /// </summary>
    void StartGroup();

    /// <summary>
    /// The end of a group (i.e., a ")" was encountered).
    /// </summary>
    /// <param name="occurences">The occurence indicator associated with the group. This 
    /// character can be "+", "*", "?" or "\0" in the case of no occurence indicator. </param>
    void EndGroup(char occurences);
    
    /// <summary>
    /// The current content model group is a sequence. This event is fired for every "," that 
    /// occurs in the model. If no ",", "|", or "#PCDATA" occurs within a group,
    /// the group is assumed to be a sequence, but this event is not fired.
    /// </summary>
    void Sequence();

    /// <summary>
    /// The current content model group is a choice. This event is fired for every "|" that 
    /// occurs in the model. If no ",", "|", or "#PCDATA" occurs within a group,
    /// the group is assumed to be a sequence. In the case of a mixed declaration, a Choice()
    /// event is fired for every occurence of "|" in the mixed content model. This allows 
    /// better precision in determining entity boundaries.
    /// </summary>
    void Choice();

    /// <summary>
    /// The current content model group is a mixed. This event is fired once for the
    /// content model where the "#PCDATA" declaration occurs. In the case of a mixed 
    /// declaration, a Choice() event is fired for every occurence of "|" in the mixed content 
    /// model. 
    /// </summary>
    void Mixed();

    /// <summary>
    /// An element particle was encountered within the current content model.
    /// </summary>
    /// <param name="name">The element particle name.</param>
    /// <param name="occurences">The occurence indicator associated with the element particle. 
    /// This character can be "+", "*", "?" or "\0" in the case of no occurence indicator. </param>
    void ElementParticle(string name, char occurences);
  }


  public class AElfredDefaultHandler : DefaultHandler, IContentModelHandler 
  {

    /* IContentModelHandler */

    public virtual void StartContentModel(string name) 
    {
      // no op
    }

    public virtual void EndContentModel()
    {
      // no op
    }
    
    public virtual void Empty()
    {
      // no op
    }

    public virtual void Any()
    {
      // no op
    }

    public virtual void StartGroup()
    {
      // no op
    }

    public virtual void EndGroup(char occurences)
    {
      // no op
    }

    public virtual void Sequence()
    {
      // no op
    }

    public virtual void Choice()
    {
      // no op
    }

    public virtual void Mixed()
    {
      // no op
    }

    public virtual void ElementParticle(string name, char occurences)
    {
      // no op
    }
  }
}
