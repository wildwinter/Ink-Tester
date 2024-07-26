using Ink.Parsed;

namespace InkTester
{
    // Want to tie source file and line to runtime, and Ink's own runtime version
    // isn't very good at it. So we hijack Ink's tag system and tag everything before we compile.
    public class LineTagger {

        private static string TAG_LINEIDX = "__line:";


        private List<Ink.Parsed.Object> _taggedItems = new();

        // File, textLineNums
        private Dictionary<string, List<int>> _textLineNums = new();


        public LineTagger() {
        }

        public void Tag(Ink.Parsed.Story parsedStory) {

            // ---- Find all the text content we are interested in ----
            List<Text> validTextObjects = new List<Text>();
            foreach(var text in parsedStory.FindAll<Text>())
            {
                // Just a newline? Ignore.
                if (text.text.Trim()=="")
                    continue;

                // If it's a tag, ignore.
                if (IsTextTag(text))
                    continue;

                // If it's a choice, ignore
                if (IsHiddenInChoice(text))
                    continue;

                // Is this inside some code? In which case we can't do anything with that.
                if (text.parent is VariableAssignment ||
                    text.parent is StringExpression) {
                    continue;
                }

                validTextObjects.Add(text);
            }

            // ---- Scan for existing IDs ----
            // Probably won't happen now, left in in case we go back to multiple root files.
            foreach(var text in validTextObjects) {
                int? lineNum = GetLineIdx(text);
                if (lineNum!=null)  // Already tagged
                    validTextObjects.Remove(text);
            }

            // For each text object we care about...
            foreach(var text in validTextObjects) {

                // Does the source already have a line num. Might happen if two things on one line.
                int? lineIdx = GetLineIdx(text);

                // Skip if there's already something on this line.
                if (lineIdx!=null) {
                    continue;
                }

                // Generate a line ID
                _taggedItems.Add(text);
                lineIdx = _taggedItems.Count-1;

                // Insert a tag with that line ID
                var tagStart = new Ink.Parsed.Tag();
                tagStart.isStart = true;
                var tagText = new Ink.Parsed.Text(TAG_LINEIDX+lineIdx);
                var tagEnd = new Ink.Parsed.Tag();
                int idx = text.parent.content.IndexOf(text)+1;
                // Reverse insert to make sure the order ends up right!
                text.parent.content.Insert(idx, tagEnd);
                text.parent.content.Insert(idx, tagText);
                text.parent.content.Insert(idx, tagStart);

                // Insert the visited line num into a map for this file.
                // So that we have a list of "important lines" that we can
                // set visit count to 0 on.
                var fileName = text.debugMetadata.fileName;
                var lineNum = text.debugMetadata.startLineNumber;
                
                if (!_textLineNums.ContainsKey(fileName)) {
                    _textLineNums[fileName] = new List<int>();
                }
                if (!_textLineNums[fileName].Contains(lineNum))
                    _textLineNums[fileName].Add(lineNum);

            }
        }


        // Checking it's a tag. Is there a StartTag earlier in the parent content?        
        private bool IsTextTag(Text text) {

            int inTag = 0;
            foreach (var sibling in text.parent.content) {
                if (sibling==text)
                    break;
                if (sibling is Tag) {
                    var tag = (Tag)sibling;
                    if (tag.isStart)
                        inTag++;
                    else
                        inTag--;
                }
            }

            return (inTag>0);
        }

        private bool IsHiddenInChoice(Text text) {

            Ink.Parsed.Object? possibleChoice = text?.parent?.parent;
            if (possibleChoice is Ink.Parsed.Choice) {
                var choice = (Ink.Parsed.Choice)possibleChoice;
                if (text?.parent == choice.choiceOnlyContent)
                    return true;
            }
            return false;
        }

        private int? GetLineIdx(Text text) {
            List<string> tags = GetTagsAfterText(text);
            if (tags.Count>0) {
                foreach(var tag in tags) {
                    if (tag.StartsWith(TAG_LINEIDX)) {
                        return int.Parse(tag.Substring(TAG_LINEIDX.Length));
                    }
                }
            }
            return null;
        }

        private List<string> GetTagsAfterText(Text text) {
        
            var tags = new List<string>();

            bool afterText = false;
            int inTag = 0;

            foreach (var sibling in text.parent.content) {
                
                // Have we hit the text we care about yet? If not, carry on.
                if (sibling==text) {
                    afterText = true;
                    continue;
                }
                if (!afterText)
                    continue;

                // Have we hit an end-of-line marker? If so, stop looking, no tags here.   
                if (sibling is Text && ((Text)sibling).text=="\n")
                    break;

                // Have we found the start or end of a tag?
                if (sibling is Tag) {
                    var tag = (Tag)sibling;
                    if (tag.isStart)
                        inTag++;
                    else
                        inTag--;
                    continue;
                }

                // Have we hit the end of a tag? Add it to our tag list!
                if ((inTag>0) && (sibling is Text)) {
                    tags.Add(((Text)sibling).text.Trim());
                } 
            }
            return tags;
        }

        // Get the parsed object associated with the tags on a given line.
        // (i.e. hopefully containing the TAG_LINEIDX we inserted.)
        public Ink.Parsed.Object? GetParsedObjectFromTags(List<string> tags) {
            foreach (var tag in tags) {
                if (tag.StartsWith(TAG_LINEIDX)) {
                    int lineIdx = int.Parse(tag.Substring(TAG_LINEIDX.Length));
                    return _taggedItems[lineIdx];
                }
            }
            return null;
        }

        // Get a list of lines we care about in this file so we can set their visit counts to 0
        public List<int> GetLineNumsForFile(string fileName) {
            return _textLineNums[fileName];
        }
    }
}