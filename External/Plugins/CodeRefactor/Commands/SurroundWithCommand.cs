using System.IO;
using System.Text;
using PluginCore;
using PluginCore.Helpers;
using PluginCore.Utilities;

namespace CodeRefactor.Commands
{
    public class SurroundWithCommand
    {
        public const string SurroundFolder = "surround";
        public const string SurroundExt = ".fds";

        protected string SnippetCode;

        public SurroundWithCommand(string snippet) => SnippetCode = snippet;

        public void Execute() => ExecutionImplementation();

        /// <summary>
        /// The actual process implementation
        /// </summary>
        protected void ExecutionImplementation()
        {
            var sci = PluginBase.MainForm.CurrentDocument?.SciControl;
            if (sci is null) return;
            sci.BeginUndoAction();
            try
            {
                string selection = sci.SelText;
                if (string.IsNullOrEmpty(selection)) return;
                if (selection.TrimStart().Length == 0) return;

                sci.SetSel(sci.SelectionStart + selection.Length - selection.TrimStart().Length, sci.SelectionEnd);
                sci.CurrentPos = sci.SelectionEnd;
                
                int lineStart = sci.LineFromPosition(sci.SelectionStart);
                int lineEnd = sci.LineFromPosition(sci.SelectionEnd);
                int firstLineIndent = sci.GetLineIndentation(lineStart);
                int entryPointIndent = 0;

                string snippet = GetSnippet(SnippetCode, sci.ConfigurationLanguage, sci.Encoding);
                int pos = snippet.IndexOfOrdinal("{0}");
                if (pos > -1)
                {
                    while (pos >= 0)
                    {
                        var c = snippet.Substring(--pos, 1);
                        if (c.Equals("\t")) entryPointIndent += sci.Indent;
                        else break;
                    }
                }

                for (int i = lineStart; i <= lineEnd; i++)
                {
                    int indent = sci.GetLineIndentation(i);
                    if (i > lineStart)
                    {
                        sci.SetLineIndentation(i, indent - firstLineIndent + entryPointIndent);
                    }
                }

                snippet = snippet.Replace("{0}", sci.SelText);

                int insertPos = sci.SelectionStart;
                int selEnd = sci.SelectionEnd;

                sci.SetSel(insertPos, selEnd);
                SnippetHelper.InsertSnippetText(sci, insertPos, snippet);
            }
            finally
            {
                sci.EndUndoAction();
            }
        }

        public static string? GetSnippet(string word, string syntax, Encoding current)
        {
            var specific = Path.Combine(PathHelper.SnippetDir, syntax, SurroundFolder, word + SurroundExt);
            if (File.Exists(specific))
            {
                var info = FileHelper.GetEncodingFileInfo(specific);
                return DataConverter.ChangeEncoding(info.Contents, info.CodePage, current.CodePage);
            }
            var global = Path.Combine(PathHelper.SnippetDir, SurroundFolder, word + SurroundExt);
            if (File.Exists(global))
            {
                var info = FileHelper.GetEncodingFileInfo(global);
                return DataConverter.ChangeEncoding(info.Contents, info.CodePage, current.CodePage);
            }
            return null;
        }
    }
}