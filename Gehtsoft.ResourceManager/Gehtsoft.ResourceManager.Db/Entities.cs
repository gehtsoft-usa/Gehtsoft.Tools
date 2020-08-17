using System;
using System.Data;
using Gehtsoft.EF.Entities;

namespace Gehtsoft.ResourceManager.Db
{
    [Entity(Scope="ResourceManager", Table="localization_components")]
    public class DbComponent
    {
        [EntityProperty(Field="id", DbType = DbType.Int32, Autoincrement = true, PrimaryKey = true)]
        public int ID { get; internal set; }
        [EntityProperty(Field = "name", DbType = DbType.String, Size=64, Sorted = true)]
        public string Name { get; set; }
    }

    [Entity(Scope = "ResourceManager", Table = "localization_language")]
    public class DbLanguage
    {
        [EntityProperty(Field = "id", DbType = DbType.Int32, Autoincrement = true, PrimaryKey = true)]
        public int ID { get; internal set; }
        [EntityProperty(Field = "name", DbType = DbType.String, Size = 64, Sorted = true)]
        public string Name { get; set; }
    }

    [Entity(Scope = "ResourceManager", Table = "localization_message")]
    public class DbMessage
    {
        [EntityProperty(Field = "id", DbType = DbType.Int32, Autoincrement = true, PrimaryKey = true)]
        public int ID { get; internal set; }
        [EntityProperty(Field = "component", ForeignKey = true)]
        public DbComponent Component { get; set; }
        [EntityProperty(Field = "name", DbType = DbType.String, Size = 64, Sorted = true)]
        public string Name { get; set; }
    }

    [Entity(Scope = "ResourceManager", Table = "localization_message_text")]
    public class DbMessageText
    {
        [EntityProperty(Field = "id", DbType = DbType.Int32, Autoincrement = true, PrimaryKey = true)]
        public int ID { get; internal set; }
        [EntityProperty(Field = "language", ForeignKey = true)]
        public DbLanguage Language { get; set; }
        [EntityProperty(Field = "message", ForeignKey = true)]
        public DbMessage Message { get; set; }
        [EntityProperty(Field = "value", DbType = DbType.String, Size = 1024, Sorted = true)]
        public string Value { get; set; }
    }
}
