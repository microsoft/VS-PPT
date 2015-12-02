using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    internal class VbParser : CsharpVBBlockParser
    {
        public VbParser()
        {
        }

        private BlockType FindType(SyntaxKind kind)
        {
            if (kind == SyntaxKind.ForBlock || kind == SyntaxKind.ForEachBlock || kind == SyntaxKind.WhileBlock ||
                kind == SyntaxKind.SimpleDoLoopBlock || kind == SyntaxKind.DoLoopUntilBlock || kind == SyntaxKind.DoLoopWhileBlock ||
                kind == SyntaxKind.DoUntilLoopBlock || kind == SyntaxKind.DoWhileLoopBlock)
                return BlockType.Loop;
            else if (kind == SyntaxKind.MultiLineIfBlock || kind == SyntaxKind.ElseBlock || kind == SyntaxKind.ElseIfBlock ||
                     kind == SyntaxKind.SelectBlock || kind == SyntaxKind.CaseBlock || kind == SyntaxKind.CaseElseBlock ||
                     kind == SyntaxKind.TryBlock || kind == SyntaxKind.CatchBlock || kind == SyntaxKind.FinallyBlock)
                return BlockType.Conditional;
            else if (kind == SyntaxKind.ClassBlock || kind == SyntaxKind.InterfaceBlock || kind == SyntaxKind.StructureBlock ||
                     kind == SyntaxKind.ModuleBlock || kind == SyntaxKind.EnumBlock)
                return BlockType.Class;
            else if (kind == SyntaxKind.NamespaceBlock)
                return BlockType.Namespace;
            else if (kind == SyntaxKind.SubBlock || kind == SyntaxKind.FunctionBlock || kind == SyntaxKind.PropertyBlock ||
                     kind == SyntaxKind.SyncLockBlock || kind == SyntaxKind.UsingBlock ||
                     kind == SyntaxKind.GetAccessorBlock || kind == SyntaxKind.SetAccessorBlock)
                return BlockType.Method;
            else
                return BlockType.Unknown;
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
                    SyntaxKind kind = SyntaxKind.None;
                    int startPosition = 0;
                    int endPosition = 0;
                    int statementEndPosition = 0;

                    if (TryAsNamespace(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsClass(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsStruct(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsInterface(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsEnum(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsModule(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsMethod(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsProperty(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsPropertyAccessor(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsIf(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsElse(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsElseIf(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsFor(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsForEach(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsSelect(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsSelectCase(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsDo(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsWhile(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsTry(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsCatch(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsFinally(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsUsing(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition) ||
                        TryAsSyncLock(childnode, ref kind, ref startPosition, ref endPosition, ref statementEndPosition))
                    {
                        var statementStart = childnode.SpanStart;
                        string statement = StatementFromSpan(snapshot, startPosition, statementEndPosition);
                        CodeBlock child = CreateCodeBlock(parentCodeBlockNode, statement, kind, new SnapshotSpan(snapshot, Span.FromBounds(startPosition, endPosition)), statementStart, level + 1);
                        ParseSyntaxNode(snapshot, childnode, child, token, level + 1);
                    }
                }
            }
        }

        private CodeBlock CreateCodeBlock(CodeBlock parent, string rawStatement, SyntaxKind kind, SnapshotSpan span, int statementStart, int level)
        {
            BlockType type = this.FindType(kind);
            CodeBlock child = new CodeBlock(parent, type, rawStatement,
                                            span, statementStart, level);

            return child;
        }

        private bool TryAsNamespace(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsNameSpace = childnode as NamespaceBlockSyntax;
            if (childAsNameSpace != null && childAsNameSpace.NamespaceStatement != null)
            {
                kind = childAsNameSpace.Kind();
                startPosition = childAsNameSpace.NamespaceStatement.SpanStart;
                endPosition = childAsNameSpace.EndNamespaceStatement.BlockKeyword.Span.End;
                statementEndPosition = childAsNameSpace.NamespaceStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsClass(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsClass = childnode as ClassBlockSyntax;
            if (childAsClass != null && childAsClass.ClassStatement != null)
            {
                kind = childAsClass.Kind();
                startPosition = childAsClass.ClassStatement.SpanStart;
                endPosition = childAsClass.EndClassStatement.Span.End;
                statementEndPosition = childAsClass.ClassStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsInterface(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsInterface = childnode as InterfaceBlockSyntax;
            if (childAsInterface != null && childAsInterface.InterfaceStatement != null)
            {
                kind = childAsInterface.Kind();
                startPosition = childAsInterface.InterfaceStatement.SpanStart;
                endPosition = childAsInterface.EndInterfaceStatement.Span.End;
                statementEndPosition = childAsInterface.InterfaceStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsStruct(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsStruct = childnode as StructureBlockSyntax;
            if (childAsStruct != null && childAsStruct.StructureStatement != null)
            {
                kind = childAsStruct.Kind();
                startPosition = childAsStruct.StructureStatement.SpanStart;
                endPosition = childAsStruct.EndStructureStatement.Span.End;
                statementEndPosition = childAsStruct.StructureStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsEnum(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsEnum = childnode as EnumBlockSyntax;
            if (childAsEnum != null && childAsEnum.EnumStatement != null)
            {
                kind = childAsEnum.Kind();
                startPosition = childAsEnum.EnumStatement.SpanStart;
                endPosition = childAsEnum.EndEnumStatement.Span.End;
                statementEndPosition = childAsEnum.EnumStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsModule(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsModule = childnode as ModuleBlockSyntax;
            if (childAsModule != null && childAsModule.ModuleStatement != null)
            {
                kind = childAsModule.Kind();
                startPosition = childAsModule.ModuleStatement.SpanStart;
                endPosition = childAsModule.EndModuleStatement.Span.End;
                statementEndPosition = childAsModule.ModuleStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsProperty(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsProperty = childnode as PropertyBlockSyntax;
            if (childAsProperty != null && childAsProperty.PropertyStatement != null)
            {
                kind = childAsProperty.Kind();
                startPosition = childAsProperty.PropertyStatement.SpanStart;
                endPosition = childAsProperty.EndPropertyStatement.Span.End;
                statementEndPosition = childAsProperty.PropertyStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsPropertyAccessor(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsPropertyAccessor = childnode as AccessorBlockSyntax;
            if (childAsPropertyAccessor != null && childAsPropertyAccessor.AccessorStatement != null)
            {
                kind = childAsPropertyAccessor.Kind();
                startPosition = childAsPropertyAccessor.AccessorStatement.SpanStart;
                endPosition = childAsPropertyAccessor.EndAccessorStatement.Span.End;
                statementEndPosition = childAsPropertyAccessor.AccessorStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsSelect(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsSelect = childnode as SelectBlockSyntax;
            if (childAsSelect != null && childAsSelect.SelectStatement != null)
            {
                kind = childAsSelect.Kind();
                startPosition = childAsSelect.SelectStatement.SpanStart;
                endPosition = childAsSelect.EndSelectStatement.Span.End;
                statementEndPosition = childAsSelect.SelectStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsSelectCase(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsSelectCase = childnode as CaseBlockSyntax;
            if (childAsSelectCase != null && childAsSelectCase.CaseStatement != null)
            {
                var parent = childAsSelectCase.Parent as SelectBlockSyntax;
                if (parent != null && parent.CaseBlocks.Count > 1 && childAsSelectCase != parent.CaseBlocks.LastOrDefault())
                {
                    kind = childAsSelectCase.Kind();
                    startPosition = childAsSelectCase.SpanStart;

                    int nextIndex = parent.CaseBlocks.IndexOf(childAsSelectCase) + 1;
                    endPosition = parent.CaseBlocks[nextIndex].SpanStart;

                    statementEndPosition = childAsSelectCase.CaseStatement.Span.End;
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsMethod(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsMethod = childnode as MethodBlockSyntax;
            if (childAsMethod != null && childAsMethod.SubOrFunctionStatement != null)
            {
                kind = childAsMethod.Kind();
                startPosition = childAsMethod.SubOrFunctionStatement.SpanStart;
                endPosition = childAsMethod.EndSubOrFunctionStatement.Span.End;
                statementEndPosition = childAsMethod.SubOrFunctionStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsIf(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsIf = childnode as MultiLineIfBlockSyntax;
            if (childAsIf != null && childAsIf.IfStatement != null)
            {
                kind = childAsIf.Kind();
                startPosition = childAsIf.IfStatement.SpanStart;
                statementEndPosition = childAsIf.IfStatement.Span.End;

                if (childAsIf.ElseBlock == null && childAsIf.ElseIfBlocks.Count == 0)
                {
                    endPosition = childAsIf.EndIfStatement.Span.End;
                }
                else if (childAsIf.ElseBlock != null)
                {
                    endPosition = childAsIf.ElseBlock.Span.Start;
                }
                else if (childAsIf.ElseIfBlocks.Count > 0)
                {
                    endPosition = childAsIf.ElseIfBlocks[0].Span.Start;
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsElse(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsElse = childnode as ElseBlockSyntax;
            if (childAsElse != null && childAsElse.ElseStatement != null)
            {
                kind = childAsElse.Kind();
                startPosition = childAsElse.ElseStatement.SpanStart;
                endPosition = ((MultiLineIfBlockSyntax)childAsElse.Parent).EndIfStatement.Span.End;
                statementEndPosition = childAsElse.ElseStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsElseIf(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsElseIf = childnode as ElseIfBlockSyntax;
            if (childAsElseIf != null && childAsElseIf.ElseIfStatement != null)
            {
                kind = childAsElseIf.Kind();
                startPosition = childAsElseIf.ElseIfStatement.SpanStart;
                statementEndPosition = childAsElseIf.ElseIfStatement.Span.End;

                var parent = childAsElseIf.Parent as MultiLineIfBlockSyntax;
                if (childAsElseIf == parent.ElseIfBlocks.LastOrDefault())
                {
                    endPosition = parent.EndIfStatement.Span.End;
                }
                else
                {
                    int nextIndex = parent.ElseIfBlocks.IndexOf(childAsElseIf) + 1;
                    endPosition = parent.ElseIfBlocks[nextIndex].Span.Start;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsFor(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsFor = childnode as ForBlockSyntax;
            if (childAsFor != null && childAsFor.ForStatement != null)
            {
                kind = childAsFor.Kind();
                startPosition = childAsFor.ForStatement.SpanStart;
                endPosition = childAsFor.NextStatement.Span.End;
                statementEndPosition = childAsFor.ForStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsForEach(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsForEach = childnode as ForEachBlockSyntax;
            if (childAsForEach != null && childAsForEach.ForEachStatement != null)
            {
                kind = childAsForEach.Kind();
                startPosition = childAsForEach.ForEachStatement.SpanStart;
                endPosition = childAsForEach.NextStatement.Span.End;
                statementEndPosition = childAsForEach.ForEachStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsDo(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsDo = childnode as DoLoopBlockSyntax;
            if (childAsDo != null && childAsDo.DoStatement != null)
            {
                kind = childAsDo.Kind();
                startPosition = childAsDo.DoStatement.SpanStart;
                endPosition = childAsDo.LoopStatement.Span.End;
                statementEndPosition = childAsDo.DoStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsWhile(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsWhile = childnode as WhileBlockSyntax;
            if (childAsWhile != null && childAsWhile.WhileStatement != null)
            {
                kind = childAsWhile.Kind();
                startPosition = childAsWhile.WhileStatement.SpanStart;
                endPosition = childAsWhile.EndWhileStatement.Span.End;
                statementEndPosition = childAsWhile.WhileStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsUsing(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsUsing = childnode as UsingBlockSyntax;
            if (childAsUsing != null && childAsUsing.UsingStatement != null)
            {
                kind = childAsUsing.Kind();
                startPosition = childAsUsing.UsingStatement.SpanStart;
                endPosition = childAsUsing.EndUsingStatement.Span.End;
                statementEndPosition = childAsUsing.UsingStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsSyncLock(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsSyncLock = childnode as SyncLockBlockSyntax;
            if (childAsSyncLock != null && childAsSyncLock.SyncLockStatement != null)
            {
                kind = childAsSyncLock.Kind();
                startPosition = childAsSyncLock.SyncLockStatement.SpanStart;
                endPosition = childAsSyncLock.EndSyncLockStatement.Span.End;
                statementEndPosition = childAsSyncLock.SyncLockStatement.Span.End;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsTry(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsTry = childnode as TryBlockSyntax;
            if (childAsTry != null && childAsTry.TryStatement != null)
            {
                kind = childAsTry.Kind();
                startPosition = childAsTry.TryStatement.SpanStart;
                statementEndPosition = childAsTry.TryStatement.Span.End;

                if (childAsTry.CatchBlocks.Count > 0)
                {
                    endPosition = childAsTry.CatchBlocks[0].SpanStart;
                }
                else if (childAsTry.FinallyBlock != null)
                {
                    endPosition = childAsTry.FinallyBlock.SpanStart;
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsCatch(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsCatch = childnode as CatchBlockSyntax;
            if (childAsCatch != null && childAsCatch.CatchStatement != null)
            {
                kind = childAsCatch.Kind();
                startPosition = childAsCatch.CatchStatement.SpanStart;
                statementEndPosition = childAsCatch.CatchStatement.Span.End;

                var parent = childAsCatch.Parent as TryBlockSyntax;
                if (parent != null && parent.CatchBlocks.Count > 0 && childAsCatch == parent.CatchBlocks.LastOrDefault())
                {
                    if (parent.FinallyBlock == null)
                    {
                        endPosition = parent.EndTryStatement.Span.End;
                    }
                    else
                    {
                        endPosition = parent.FinallyBlock.SpanStart;
                    }
                }
                else if (parent != null && parent.CatchBlocks.Count > 1)
                {
                    int nextIndex = parent.CatchBlocks.IndexOf(childAsCatch) + 1;
                    endPosition = parent.CatchBlocks[nextIndex].SpanStart;
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryAsFinally(SyntaxNode childnode, ref SyntaxKind kind, ref int startPosition, ref int endPosition, ref int statementEndPosition)
        {
            var childAsFinally = childnode as FinallyBlockSyntax;
            if (childAsFinally != null && childAsFinally.FinallyStatement != null)
            {
                var parent = childAsFinally.Parent as TryBlockSyntax;
                if (parent != null)
                {
                    kind = childAsFinally.Kind();
                    startPosition = childAsFinally.FinallyStatement.SpanStart;
                    endPosition = parent.EndTryStatement.Span.End;
                    statementEndPosition = childAsFinally.FinallyStatement.Span.End;
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
