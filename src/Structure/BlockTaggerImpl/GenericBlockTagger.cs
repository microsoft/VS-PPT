using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    public sealed class GenericBlockTagger : ITagger<IBlockTag>
    {
        #region private
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        private class BackgroundScan
        {
            public CancellationTokenSource CancellationSource = new CancellationTokenSource();

            public delegate void CompletionCallback(CodeBlock root);

            /// <summary>
            /// Does a background scan in <paramref name="snapshot"/>. Call
            /// <paramref name="completionCallback"/> once the scan has completed.
            /// </summary>
            /// <param name="snapshot">Text snapshot in which to scan.</param>
            /// <param name="completionCallback">Delegate to call if the scan is completed (will be called on the UI thread).</param>
            /// <remarks>The constructor must be called from the UI thread.</remarks>
            public BackgroundScan(ITextSnapshot snapshot, IParser parser, CompletionCallback completionCallback)
            {
                Task.Run(async delegate
                {
                    CodeBlock newRoot = await parser.ParseAsync(snapshot, this.CancellationSource.Token);

                    if ((newRoot != null) && !this.CancellationSource.Token.IsCancellationRequested)
                        completionCallback(newRoot);
                });
            }

            public void Cancel()
            {
                if (this.CancellationSource != null)
                {
                    this.CancellationSource.Cancel();
                    this.CancellationSource.Dispose();
                }
            }
        }

        private ITextBuffer _buffer;
        private IParser _parser;
        private BackgroundScan _scan;
        private CodeBlock _root;

        private int _refCount;
        public void AddRef()
        {
            if (++_refCount == 1)
            {
                _buffer.Changed += OnChanged;
                this.ScanBuffer(_buffer.CurrentSnapshot);
            }
        }
        public void Release()
        {
            if (--_refCount == 0)
            {
                _buffer.Changed -= OnChanged;

                if (_scan != null)
                {
                    //Stop and blow away the old scan (even if it didn't finish, the results are not interesting anymore).
                    _scan.Cancel();
                    _scan = null;
                }
                _root = null; //Allow the old root to be GC'd
            }
        }

        private void OnChanged(object sender, TextContentChangedEventArgs e)
        {
            if (AnyTextChanges(e.Before.Version, e.After.Version))
                this.ScanBuffer(e.After);
        }

        private static bool AnyTextChanges(ITextVersion oldVersion, ITextVersion currentVersion)
        {
            while (oldVersion != currentVersion)
            {
                if (oldVersion.Changes.Count > 0)
                    return true;
                oldVersion = oldVersion.Next;
            }

            return false;
        }

        private void ScanBuffer(ITextSnapshot snapshot)
        {
            if (_scan != null)
            {
                //Stop and blow away the old scan (even if it didn't finish, the results are not interesting anymore).
                _scan.Cancel();
                _scan = null;
            }

            //The underlying buffer could be very large, meaning that doing the scan for all matches on the UI thread
            //is a bad idea. Do the scan on the background thread and use a callback to raise the changed event when
            //the entire scan has completed.
            _scan = new BackgroundScan(snapshot, _parser,
                                            delegate (CodeBlock newRoot)
                                            {
                                                //This delegate is executed on a background thread.
                                                _root = newRoot;

                                                EventHandler<SnapshotSpanEventArgs> handler = this.TagsChanged;
                                                if (handler != null)
                                                    handler(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
                                            });
        }
        #endregion

        public GenericBlockTagger(ITextBuffer buffer, IParser parser)
        {
            _buffer = buffer;
            _parser = parser;
        }

        public IEnumerable<ITagSpan<IBlockTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            CodeBlock root = _root;  //this.root could be set on a background thread, so get a snapshot.
            if (root != null)
            {
                if (root.Span.Snapshot != spans[0].Snapshot)
                {
                    //There is a version skew between when the parse was done and what is being asked for.
                    IList<SnapshotSpan> translatedSpans = new List<SnapshotSpan>(spans.Count);
                    foreach (var span in spans)
                        translatedSpans.Add(span.TranslateTo(root.Span.Snapshot, SpanTrackingMode.EdgeInclusive));

                    spans = new NormalizedSnapshotSpanCollection(translatedSpans);
                }

                foreach (var child in root.Children)
                {
                    foreach (var tag in GetTags(child, spans))
                        yield return tag;
                }
            }
        }

        private static IEnumerable<ITagSpan<IBlockTag>> GetTags(CodeBlock block, NormalizedSnapshotSpanCollection spans)
        {
            if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(block.Span)))
            {
                yield return new TagSpan<IBlockTag>(block.Span, block);

                foreach (var child in block.Children)
                {
                    foreach (var tag in GetTags(child, spans))
                        yield return tag;
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
