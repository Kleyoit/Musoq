﻿using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema
{
    public class TestSchema<T> : SchemaBase
        where T : BasicEntity
    {
        private readonly MethodsAggregator _aggreagator;
        private readonly IEnumerable<T> _sources;

        private static readonly IDictionary<string, int> TestNameToIndexMap;
        private static readonly IDictionary<int, Func<T, object>> TestIndexToObjectAccessMap;

        static TestSchema()
        {
            TestNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(BasicEntity.Name), 10},
                {nameof(BasicEntity.City), 11},
                {nameof(BasicEntity.Country), 12},
                {nameof(BasicEntity.Population), 13},
                {nameof(BasicEntity.Self), 14},
                {nameof(BasicEntity.Money), 15},
                {nameof(BasicEntity.Month), 16},
                {nameof(BasicEntity.Time), 17},
                {nameof(BasicEntity.Id), 18}
            };

            TestIndexToObjectAccessMap = new Dictionary<int, Func<T, object>>
            {
                {10, arg => arg.Name},
                {11, arg => arg.City},
                {12, arg => arg.Country},
                {13, arg => arg.Population},
                {14, arg => arg.Self},
                {15, arg => arg.Money},
                {16, arg => arg.Month},
                {17, arg => arg.Time},
                {18, arg => arg.Id}
            };
        }

        public TestSchema(IEnumerable<T> sources)
        : base("test",CreateLibrary())
        {
            _sources = sources;
        }

        private static MethodsAggregator CreateLibrary()
        {

            var methodManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var lib = new TestLibrary();

            propertiesManager.RegisterProperties(lib);
            methodManager.RegisterLibraries(lib);

            return new MethodsAggregator(methodManager, propertiesManager);
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new BasicEntityTable();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new EntitySource<T>(_sources, TestNameToIndexMap, TestIndexToObjectAccessMap);
        }
    }
}