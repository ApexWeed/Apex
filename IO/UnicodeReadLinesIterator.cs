using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apex.IO
{
    internal class UnicodeReadLinesIterator : Iterator<string>
    {
        private readonly string path;
        private readonly Encoding encoding;
        private StreamReader reader;

        public UnicodeReadLinesIterator(string Path, Encoding Encoding, StreamReader Reader)
        {
            path = Path;
            encoding = Encoding;
            reader = Reader;
        }

        protected override Iterator<string> Clone()
        {
            return CreateIterator(path, encoding, reader);
        }

        public override bool MoveNext()
        {
            if (reader != null)
            {
                current = reader.ReadLine();

                return current != null;
            }

            return false;
        }

        internal static UnicodeReadLinesIterator CreateIterator(string Path, Encoding Encoding)
        {
            return CreateIterator(Path, Encoding, null);
        }

        internal static UnicodeReadLinesIterator CreateIterator(string Path, Encoding Encoding, StreamReader Reader)
        {
            return new UnicodeReadLinesIterator(Path, Encoding, Reader ?? UnicodeFile.OpenText(Path, Encoding));
        }
    }
}
