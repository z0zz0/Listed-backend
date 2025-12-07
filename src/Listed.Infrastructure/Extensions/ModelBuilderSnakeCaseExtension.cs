using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Listed.Infrastructure.Extensions;

public static class ModelBuilderSnakeCaseExtensions
{
    public static void UseSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table
            var tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
                entity.SetTableName(ToSnakeCase(tableName));

            // Columns
            var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (!string.IsNullOrWhiteSpace(columnName))
                    property.SetColumnName(ToSnakeCase(columnName));
            }

            // Keys
            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                    key.SetName(ToSnakeCase(keyName));
            }

            // FKs
            foreach (var fk in entity.GetForeignKeys())
            {
                var fkName = fk.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(fkName))
                    fk.SetConstraintName(ToSnakeCase(fkName));
            }

            // Indexes
            foreach (var index in entity.GetIndexes())
            {
                var dbName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(dbName))
                    index.SetDatabaseName(ToSnakeCase(dbName));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var chars = new List<char>(name.Length + 10);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var hasPrev = i > 0;
                var hasNext = i + 1 < name.Length;

                if (hasPrev && (char.IsLower(name[i - 1]) || (hasNext && char.IsLower(name[i + 1]))))
                    chars.Add('_');

                chars.Add(char.ToLowerInvariant(c));
            }
            else
            {
                chars.Add(c);
            }
        }
        return new string(chars.ToArray());
    }
}

