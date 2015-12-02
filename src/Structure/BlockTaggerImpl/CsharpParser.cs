using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    internal class CsharpParser : CsharpVBBlockParser
    {
        public CsharpParser()
        {
        }

        protected override void ParseSyntaxNode(ITextSnapshot snapshot, SyntaxNode parentSyntaxNode, CodeBlock parentCodeBlockNode, CancellationToken token, int level)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            else
            {
                foreach (var childnode in parentSyntaxNode.ChildNodes())
                {
                    BlockType type = BlockType.Unknown;
                    int startPosition = 0;
                    int endPosition = 0;

                    if (TryAsNamespace(childnode, ref type, ref startPosition, ref endPosition) ||
                        TryAsType(childnode, ref type, ref startPosition, ref endPosition) ||
                        TryAsEnum(childnode, ref type, ref startPosition, ref endPosition) ||
                        TryAsSwitch(childnode, ref type, ref startPosition, ref endPosition) ||
                        TryAsSwitchSection(childnode, snapshot, ref type, ref startPosition, ref endPosition) ||
                        TryAsProperty(childnode, ref type, ref startPosition, ref endPosition))
                    {
                        var statementStart = childnode.SpanStart;
                        string statement = StatementFromSpan(snapshot, statementStart, startPosition);

                        CodeBlock child = new CodeBlock(parentCodeBlockNode, type, statement, new SnapshotSpan(snapshot, Span.FromBounds(startPosition, endPosition)), statementStart, level + 1);
                        ParseSyntaxNode(snapshot, childnode, child, token, level + 1);
                    }
                    else if (TryAsBlock(childnode, parentSyntaxNode, ref type, ref startPosition, ref endPosition))
                    {
                        int statementStart = (type == BlockType.Unknown) ? startPosition : parentSyntaxNode.SpanStart;
                        string statement = StatementFromSpan(snapshot, statementStart, startPosition);

                        CodeBlock child = new CodeBlock(parentCodeBlockNode, type, statement, new SnapshotSpan(snapshot, Span.FromBounds(startPosition, endPosition)), statementStart, level + 1);
                        ParseSyntaxNode(snapshot, childnode, child, token, level + 1);
                    }
                    else
                    {
                        ParseSyntaxNode(snapshot, childnode, parentCodeBlockNode, token, level);
                    }

                }
            }
        }

        private static BlockType FindType(SyntaxKind kind)
        {
            if (kind == SyntaxKind.ForEachStatement || kind == SyntaxKind.ForStatement ||
                kind == SyntaxKind.WhileStatement || kind == SyntaxKind.DoStatement)
                return BlockType.Loop;
            else if (kind == SyntaxKind.IfStatement || kind == SyntaxKind.ElseClause || kind == SyntaxKind.SwitchStatement)
                return BlockType.Conditional;
            else if (kind == SyntaxKind.ClassDeclaration || kind == SyntaxKind.InterfaceDeclaration ||
                     kind == SyntaxKind.StructDeclaration || kind == SyntaxKind.EnumDeclaration)
                return BlockType.Class;
            else if (kind == SyntaxKind.NamespaceDeclaration)
                return BlockType.Namespace;
            else if (kind == SyntaxKind.MethodDeclaration || kind == SyntaxKind.PropertyDeclaration ||
                     kind == SyntaxKind.GetAccessorDeclaration || kind == SyntaxKind.SetAccessorDeclaration ||
                     kind == SyntaxKind.ParenthesizedLambdaExpression)
                return BlockType.Method;
            else if (kind == SyntaxKind.TryStatement || kind == SyntaxKind.CatchClause || kind == SyntaxKind.FinallyClause || kind == SyntaxKind.LockStatement)
                return BlockType.Other;
            else
                return BlockType.Unknown;
        }

        private bool TryAsNamespace(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as NamespaceDeclarationSyntax;
            if (child != null)
            {
                type = BlockType.Namespace;
                startPosition = child.OpenBraceToken.SpanStart;
                endPosition = child.CloseBraceToken.Span.End;

                return true;
            }

            return false;
        }

        private bool TryAsType(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as TypeDeclarationSyntax;
            if (child != null)
            {
                type = BlockType.Class;
                startPosition = child.OpenBraceToken.SpanStart;
                endPosition = child.CloseBraceToken.Span.End;

                return true;
            }

            return false;
        }

        private bool TryAsEnum(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as EnumDeclarationSyntax;
            if (child != null)
            {
                type = BlockType.Class;
                startPosition = child.OpenBraceToken.SpanStart;
                endPosition = child.CloseBraceToken.Span.End;

                return true;
            }

            return false;
        }

        private bool TryAsMethod(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as MethodDeclarationSyntax;
            if (child != null)
            {
                var body = child.Body;
                if (body != null)
                {
                    type = BlockType.Method;
                    startPosition = child.Body.OpenBraceToken.SpanStart;
                    endPosition = child.Body.CloseBraceToken.Span.End;

                    return true;
                }
            }

            return false;
        }

        private bool TryAsSwitch(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as SwitchStatementSyntax;
            if (child != null)
            {
                type = BlockType.Conditional;
                startPosition = child.OpenBraceToken.SpanStart;
                endPosition = child.CloseBraceToken.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsSwitchSection(SyntaxNode childnode, ITextSnapshot snapshot, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as SwitchSectionSyntax;
            if (child != null)
            {
                type = BlockType.Conditional;

                startPosition = child.Labels.FullSpan.End;
                ITextSnapshotLine line = snapshot.GetLineFromPosition(startPosition);
                if ((startPosition == line.Start.Position) && (line.LineNumber > 0))
                {
                    startPosition = snapshot.GetLineFromLineNumber(line.LineNumber - 1).End;
                }

                endPosition = child.Span.End;

                return true;
            }

            return false;
        }

        private bool TryAsProperty(SyntaxNode childnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as PropertyDeclarationSyntax;
            if (child != null)
            {
                type = BlockType.Method;
                startPosition = child.AccessorList.OpenBraceToken.SpanStart;
                endPosition = child.AccessorList.CloseBraceToken.Span.End;

                return true;
            }

            return false;
        }

        private bool TryAsBlock(SyntaxNode childnode, SyntaxNode parentnode, ref BlockType type, ref int startPosition, ref int endPosition)
        {
            var child = childnode as BlockSyntax;
            if (child != null)
            {
                type = FindType(parentnode.Kind());

                startPosition = child.OpenBraceToken.SpanStart;
                endPosition = child.CloseBraceToken.Span.End;

                return true;
            }

            return false;
        }
    }
}
