using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Tobi.Common._UnusedCode
{
    /// <summary>
    /// This class represents the possible option for search operation.
    /// </summary>
    [Flags]
    public enum FindOptions
    {
        /// <summary>
        /// Perform case-insensitive non-word search.
        /// </summary>
        None = 0x0000,
        /// <summary>
        /// Perform case-sensitive search.
        /// </summary>
        MatchCase = 0x0001,
        /// <summary>
        /// Perform the search against whole word.
        /// </summary>
        MatchWholeWord = 0x0002,
    }

    /// <summary>
    /// This class encapsulates the find and replace operations for <see cref="FlowDocument"/>.
    /// </summary>
    public sealed class FindAndReplaceManager
    {
        private TextRange inputTextRange;
        private TextPointer currentPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindReplaceManager"/> class given the specified <see cref="FlowDocument"/> instance.
        /// </summary>
        /// <param name="inputDocument">the input document</param>
        public FindAndReplaceManager(TextRange inputDocument)
        {
            if (inputDocument == null)
            {
                throw new ArgumentNullException("documentToFind");
            }

            this.inputTextRange = inputDocument;
            this.currentPosition = inputDocument.Start;
        }

        /// <summary>
        /// Gets and sets the offset position for the<see cref="FindReplaceManager"/>
        /// </summary>
        public TextPointer CurrentPosition
        {
            get
            {
                return currentPosition;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.CompareTo(inputTextRange.Start) < 0 || value.CompareTo(inputTextRange.End) > 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                currentPosition = value;
            }
        }

        /// <summary>
        /// Find next match of the input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="findOptions">the search options</param>
        /// <returns>The <see cref="TextRange"/> instance representing the input string.</returns>
        /// <remarks>
        /// This method will advance the <see cref="CurrentPosition"/> to next context position.
        /// </remarks>
        public TextRange FindNext(String input, FindOptions findOptions)
        {
            TextRange textRange = GetTextRangeFromPosition(ref currentPosition, input, findOptions, LogicalDirection.Forward);
            return textRange;
        }
        //public TextRange FindPrevious(String input, FindOptions findOptions)
        //{
        //    TextRange textRange = GetTextRangeFromPosition(ref currentPosition, input, findOptions, LogicalDirection.Backward);
        //    return textRange;
        //}

        /// <summary>
        /// Find all matches of the input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="findOptions">the search options</param>
        /// <returns>The <see cref="TextRange"/> instance representing the input string.</returns>
        /// <remarks>
        /// This method will advance the <see cref="CurrentPosition"/> to next context position.
        /// </remarks>
        public IEnumerable<TextRange> FindAll(String input, FindOptions findOptions)
        {
            TextPointer backup = currentPosition;
            this.currentPosition = inputTextRange.Start;

            while (currentPosition.CompareTo(inputTextRange.End) < 0)
            {
                TextRange textRange = FindNext(input, findOptions);
                if (textRange != null && !String.IsNullOrEmpty(textRange.Text))
                {
                    //Console.WriteLine(textRange.Text);
                    yield return textRange;
                }
            }

            this.currentPosition = backup;
        }

        /// <summary>
        /// Within a specified input string, replaces the input string that 
        /// match a regular expression pattern with a specified replacement string. 
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="findOptions"> the search options</param>
        /// <returns>The <see cref="TextRange"/> instance representing the replacement string.</returns>
        /// <remarks>
        /// This method will advance the <see cref="CurrentPosition"/> to next context position.
        /// </remarks>
        public TextRange Replace(String input, String replacement, FindOptions findOptions)
        {
            TextRange textRange = FindNext(input, findOptions);
            if (textRange != null)
            {
                textRange.Text = replacement;
            }

            return textRange;
        }

        /// <summary>
        /// Within a specified input string, replaces all the input strings that 
        /// match a regular expression pattern with a specified replacement string. 
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="findOptions"> the search options</param>
        /// <param name="action">the action performed for each match of the input string.</param>
        /// <returns>The number of times the replacement can occur.</returns>
        /// <remarks>
        /// This method will advance the <see cref="CurrentPosition"/> to last context position.
        /// </remarks>
        public Int32 ReplaceAll(String input, String replacement, FindOptions findOptions, Action<TextRange> action)
        {
            Int32 count = 0;
            currentPosition = inputTextRange.Start;
            while (currentPosition.CompareTo(inputTextRange.End) < 0)
            {
                TextRange textRange = Replace(input, replacement, findOptions);
                if (textRange != null)
                {
                    count++;
                    if (action != null)
                    {
                        action(textRange);
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Find the corresponding <see cref="TextRange"/> instance 
        /// representing the input string given a specified text pointer position.
        /// </summary>
        /// <param name="position">the current text position</param>
        /// <param name="textToFind">input text</param>
        /// <param name="findOptions">the search option</param>
        /// <returns>An <see cref="TextRange"/> instance represeneting the matching string withing the text container.</returns>
        private TextRange GetTextRangeFromPosition(ref TextPointer position, String input, FindOptions findOptions, LogicalDirection logicalDirection)
        {
            Boolean matchCase = (findOptions & FindOptions.MatchCase) == FindOptions.MatchCase;
            Boolean matchWholeWord = (findOptions & FindOptions.MatchWholeWord) == FindOptions.MatchWholeWord;

            TextRange textRange = null;

            while (position != null)
            {
                if (position.CompareTo(inputTextRange.End) == 0)
                {
                    break;
                }

                if (position.GetPointerContext(logicalDirection) == TextPointerContext.Text)
                {
                    String textRun = position.GetTextInRun(LogicalDirection.Forward);
                    StringComparison stringComparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    Int32 indexInRun = textRun.IndexOf(input, stringComparison);

                    if (indexInRun >= 0)
                    {
                        position = position.GetPositionAtOffset(indexInRun);
                        TextPointer nextPointer = position.GetPositionAtOffset(input.Length);
                        textRange = new TextRange(position, nextPointer);

                        if (matchWholeWord)
                        {
                            if (IsWholeWord(textRange)) // Test if the "textRange" represents a word.
                            {
                                // If a WholeWord match is found, directly terminate the loop.
                                position = position.GetPositionAtOffset(input.Length);
                                break;
                            }
                            else
                            {
                                // If a WholeWord match is not found, go to next recursion to find it.
                                position = position.GetPositionAtOffset(input.Length);
                                return GetTextRangeFromPosition(ref position, input, findOptions, logicalDirection);
                            }
                        }
                        else
                        {
                            // If a none-WholeWord match is found, directly terminate the loop.
                            position = position.GetPositionAtOffset(input.Length);
                            break;
                        }
                    }
                    else
                    {
                        // If a match is not found, go over to the next context position after the "textRun".
                        position = position.GetPositionAtOffset(textRun.Length);
                    }
                }
                else
                {
                    //If the current position doesn't represent a text context position, go to the next context position.
                    // This can effectively ignore the formatting or emebed element symbols.
                    position = position.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return textRange;
        }

        /// <summary>
        /// Determine if the specified character is a valid word character.
        /// Here only underscores, letters, and digits are considered to be valid.
        /// </summary>
        /// <param name="character">character specified</param>
        /// <returns>Boolean value didicates if the specified character is a valid word character</returns>
        private Boolean IsWordChar(Char character)
        {
            return Char.IsLetterOrDigit(character) || character == '_';
        }

        /// <summary>
        /// Test if the string within the specified <see cref="TextRange"/>instance is a word.
        /// </summary>
        /// <param name="textRange"><see cref="TextRange"/>instance to test</param>
        /// <returns>test result</returns>
        private Boolean IsWholeWord(TextRange textRange)
        {
            Char[] chars = new Char[1];

            if (textRange.Start.CompareTo(inputTextRange.Start) == 0 || textRange.Start.IsAtLineStartPosition)
            {
                textRange.End.GetTextInRun(LogicalDirection.Forward, chars, 0, 1);
                return !IsWordChar(chars[0]);
            }
            else if (textRange.End.CompareTo(inputTextRange.End) == 0)
            {
                textRange.Start.GetTextInRun(LogicalDirection.Backward, chars, 0, 1);
                return !IsWordChar(chars[0]);
            }
            else
            {
                textRange.End.GetTextInRun(LogicalDirection.Forward, chars, 0, 1);
                if (!IsWordChar(chars[0]))
                {
                    textRange.Start.GetTextInRun(LogicalDirection.Backward, chars, 0, 1);
                    return !IsWordChar(chars[0]);
                }
            }

            return false;
        }
    }
}