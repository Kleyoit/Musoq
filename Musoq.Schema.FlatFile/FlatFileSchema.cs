﻿using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Schema.FlatFile
{
    public class FlatFileSchema : SchemaBase
    {
        private const string SchemaName = "FlatFile";

        public FlatFileSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileTable();
            }

            throw new TableNotFoundException(nameof(name));
        }

        public override RowSource GetRowSource(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileSource((string)parameters[0]);
            }

            throw new SourceNotFoundException(nameof(name));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new FlatFileLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}