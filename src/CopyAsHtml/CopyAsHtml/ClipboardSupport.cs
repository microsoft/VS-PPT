using System.Text;
using System.Windows;

// Parts of this code taken from Mike Stall's (jmstall) blog with his permission:
// http://blogs.msdn.com/jmstall/archive/2007/01/21/html-clipboard.aspx

namespace Microsoft.VisualStudio.Text.Formatting.Implementation
{
    /// <summary>
    /// Adds support for placing data on Clipboard in HTML format.
    /// Deals with the weirdness of the HTML format and some 
    /// implementation details.
    /// Preserves other existing formats on clipboard (just adds the HTML format).
    /// </summary>
    internal static class ClipboardSupport
    {
        /// <summary>
        /// A data format used to tag the contents of the clipboard so that it's clear
        /// the data has been put in the clipboard by our editor
        /// </summary>
        private const string ClipboardLineBasedCutCopyTag = "VisualStudioEditorOperationsLineCutCopyClipboardTag";

        /// <summary>
        /// A data format used to tag the contents of the clipboard as a box selection.
        /// This is the same string that was used in VS9 and previous versions.
        /// </summary>
        private const string BoxSelectionCutCopyTag = "MSDEVColumnSelect";

        /// <summary>
        /// Places the provided data on the clipboard overriding what is currently in the clipboard.
        /// </summary>
        /// <param name="isSingleLine">Indicates whether a single line was automatically cut/copied by
        /// the editor. If <c>true</c> the clipboard contents are tagged with a special moniker.</param>
        public static void SetClipboardData(string html, string rtf, string unicode, bool isSingleLine, bool isBoxCopy)
        {
            DataObject data = new DataObject();

            if (unicode != null)
            {
                data.SetText(unicode, TextDataFormat.UnicodeText);
                data.SetText(unicode, TextDataFormat.Text);
            }

            if (html != null)
            {
                data.SetText(GetHtmlForClipboard(html), TextDataFormat.Html);
            }

            if (rtf != null)
            {
                data.SetText(rtf, TextDataFormat.Rtf);
            }

            if (isSingleLine)
            {
                data.SetData(ClipboardLineBasedCutCopyTag, new object());
            }

            if (isBoxCopy)
            {
                data.SetData(BoxSelectionCutCopyTag, new object());
            }

            try
            {
                // Use delay rendering to set the data in the clipboard to prevent 2 clipboard change
                // notifications to clipboard change listeners.
                Clipboard.SetDataObject(data, false);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
            }
        }

        private static string GetHtmlForClipboard(string htmlFragment)
        {
            StringBuilder sb = new StringBuilder();

            // Builds the CF_HTML header. See format specification here:
            // http://msdn.microsoft.com/library/default.asp?url=/workshop/networking/clipboard/htmlclipboard.asp

            // The string contains index references to other spots in the string, so we need placeholders so we can compute the offsets. 
            // The <<<<<<<_ strings are just placeholders. We'll backpatch them actual values afterwards.
            // The string layout (<<<) also ensures that it can't appear in the body of the html because the <
            // character must be escaped.
            string header =
    @"Version:1.0
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<4
";

            string pre =
    @"<!DOCTYPE html>
<HTML><HEAD><TITLE>Snippet</TITLE></HEAD><BODY><!--StartFragment-->";

            string post = @"<!--EndFragment--></BODY></HTML>";

            sb.Append(header);
            int startHTML = sb.Length;

            sb.Append(pre);
            int fragmentStart = startHTML + pre.Length;

            sb.Append(htmlFragment);
            int fragmentEnd = fragmentStart + GetByteCount(htmlFragment);

            sb.Append(post);
            int endHTML = fragmentEnd + post.Length;

            // Backpatch offsets
            sb.Replace("<<<<<<<1", To8DigitString(startHTML));
            sb.Replace("<<<<<<<2", To8DigitString(endHTML));
            sb.Replace("<<<<<<<3", To8DigitString(fragmentStart));
            sb.Replace("<<<<<<<4", To8DigitString(fragmentEnd));

            // Finally copy to clipboard.
            string data = sb.ToString();
            return data;
        }

        private static int GetByteCount(string fragment)
        {
            int result = Encoding.UTF8.GetByteCount(fragment);
            return result;
        }

        private static string To8DigitString(int x)
        {
            return string.Format("{0,8}", x.ToString("D8"));
        }
    }
}
