﻿using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Csv
{
    public class CsvSchema : SchemaBase
    {
        private const string FileTable = "file";
        private const string SchemaName = "csv";

        public CsvSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            if (name.ToLowerInvariant() == FileTable)
            {
                parameters[3] = Convert.ToInt32(parameters[3]);
                return (CsvBasedTable)Activator.CreateInstance(typeof(CsvBasedTable), parameters);
            }

            throw new NotSupportedException();
        }

        public override RowSource GetRowSource(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FileTable:
                    parameters[3] = Convert.ToInt32(parameters[3]);
                    return (CsvSource) Activator.CreateInstance(typeof(CsvSource), parameters);
            }

            throw new NotSupportedException();
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new CsvLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}