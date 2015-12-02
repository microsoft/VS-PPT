using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PowerToolsEx.BlockTagger.Implementation
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("CSharp")]
    [TagType(typeof(IBlockTag))]
    internal class CsharpBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (typeof(T) == typeof(IBlockTag))
            {
                GenericBlockTagger tagger = buffer.Properties.GetOrCreateSingletonProperty<GenericBlockTagger>(typeof(CsharpBlockTaggerProvider), delegate
                {
                    return new GenericBlockTagger(buffer, new CsharpParser());
                });

                return new DisposableTagger(tagger) as ITagger<T>;
            }
            else
                return null;
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("C/C++")]
    [TagType(typeof(IBlockTag))]
    internal class CppBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (typeof(T) == typeof(IBlockTag))
            {
                GenericBlockTagger tagger = buffer.Properties.GetOrCreateSingletonProperty<GenericBlockTagger>(typeof(CppBlockTaggerProvider), delegate
                {
                    return new GenericBlockTagger(buffer, new CppParser());
                });

                return new DisposableTagger(tagger) as ITagger<T>;
            }
            else
                return null;
        }
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("Basic")]
    [TagType(typeof(IBlockTag))]
    internal class VbBlockTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (typeof(T) == typeof(IBlockTag))
            {
                GenericBlockTagger tagger = buffer.Properties.GetOrCreateSingletonProperty<GenericBlockTagger>(typeof(VbBlockTaggerProvider), delegate
                                            {
                                                return new GenericBlockTagger(buffer, new VbParser());
                                            });

                return new DisposableTagger(tagger) as ITagger<T>;
            }
            else
                return null;
        }
    }

    internal class DisposableTagger : ITagger<IBlockTag>, IDisposable
    {
        private GenericBlockTagger _tagger;
        public DisposableTagger(GenericBlockTagger tagger)
        {
            _tagger = tagger;
            _tagger.AddRef();
            _tagger.TagsChanged += OnTagsChanged;
        }

        private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            EventHandler<SnapshotSpanEventArgs> handler = this.TagsChanged;
            if (handler != null)
                handler(sender, e);
        }

        public IEnumerable<ITagSpan<IBlockTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _tagger.GetTags(spans);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            if (_tagger != null)
            {
                _tagger.TagsChanged -= OnTagsChanged;
                _tagger.Release();
                _tagger = null;
            }
        }
    }
}