using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables
{
    public class RowResolver : IObjectResolver
    {
        private readonly IDictionary<string, int> _nameToIndexMap;
        private readonly Row _row;

        public RowResolver(Row row, IDictionary<string, int> nameToIndexMap)
        {
            _row = row;
            _nameToIndexMap = nameToIndexMap;
        }

        public object Context => _row;

        public bool HasColumn(string name)
        {
            return _nameToIndexMap.ContainsKey(name);
        }

        object IObjectResolver.this[string name] => _row[_nameToIndexMap[name]];

        object IObjectResolver.this[int index] => _row[index];
    }
}