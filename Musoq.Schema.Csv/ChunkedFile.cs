﻿using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using FQL.Schema.DataSources;

namespace FQL.Schema.Csv
{
    public class ChunkedFile : IEnumerable<IObjectResolver>
    {
        private readonly BlockingCollection<List<EntityResolver<string[]>>> _readedRows;
        private readonly CancellationToken _token;

        public ChunkedFile(BlockingCollection<List<EntityResolver<string[]>>> readedRows, CancellationToken token)
        {
            this._readedRows = readedRows;
            this._token = token;
        }

        public IEnumerator<IObjectResolver> GetEnumerator()
        {
            return new ChunkEnumerator(_readedRows, _token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}