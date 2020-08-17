using System;
using System.Collections.Generic;
using System.Linq;
using Gehtsoft.EF.Db.SqlDb;
using Gehtsoft.EF.Db.SqlDb.EntityQueries;
using Gehtsoft.EF.Entities;

namespace Gehtsoft.ResourceManager.Db
{
    public static class MessagesDao
    {
        private static Type[] gTypes = {typeof(DbComponent), typeof(DbLanguage), typeof(DbMessage), typeof(DbMessageText)};

        public static void CreateTables(SqlDbConnection connection)
        {
            foreach (Type type in gTypes)
            {
                using (EntityQuery query = connection.GetCreateEntityQuery(type))
                    query.Execute();
            }
        }

        public static void DropTables(SqlDbConnection connection)
        {
            foreach (Type type in gTypes.Reverse())
            {
                using (EntityQuery query = connection.GetDropEntityQuery(type))
                    query.Execute();
            }
        }

        public static DbComponent NewComponent()
        {
            return new DbComponent();
        }

        public static DbComponent ReadComponent(SqlDbConnection connection, int id)
        {
            using (SelectEntitiesQuery query = connection.GetSelectEntitiesQuery(typeof(DbComponent)))
            {
                query.AddWhereFilter(nameof(DbComponent.ID), CmpOp.Eq, id);
                query.Execute();
                return query.ReadOne<DbComponent>();
            }
        }

        public static DbComponent FindComponent(SqlDbConnection connection, string name)
        {
            using (SelectEntitiesQuery query = connection.GetSelectEntitiesQuery(typeof(DbComponent)))
            {
                query.AddWhereFilter(nameof(DbComponent.Name), CmpOp.Eq, name);
                query.Execute();
                return query.ReadOne<DbComponent>();
            }
        }


        public static void SaveComponent(SqlDbConnection connection, DbComponent value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using (ModifyEntityQuery query = value.ID < 1 ? connection.GetInsertEntityQuery(typeof(DbComponent)) : connection.GetUpdateEntityQuery(typeof(DbComponent)))
                query.Execute(value);
        }


        public static void DeleteComponent(SqlDbConnection connection, DbComponent value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            SelectEntityQueryBuilderBase selectBuilder = new SelectEntityQueryBuilderBase(typeof(DbMessage), connection);
            selectBuilder.AddToResultset(nameof(DbMessage.ID));
            selectBuilder.AddWhereFilter(nameof(DbMessage.Component), CmpOp.Eq, "component");

            DeleteEntityQueryBuilder deleteBuilder = new DeleteEntityQueryBuilder(typeof(DbMessageText), connection);
            deleteBuilder.AddWhereFilter(nameof(DbMessageText.Message), CmpOp.In, selectBuilder);

            using (SqlDbQuery query = connection.GetQuery(deleteBuilder))
            {
                query.BindParam("component", value.ID);
                query.ExecuteNoData();
            }

            deleteBuilder = new DeleteEntityQueryBuilder(typeof(DbMessage), connection);
            deleteBuilder.AddWhereFilter(nameof(DbMessage.Component), CmpOp.Eq, "component");

            using (SqlDbQuery query = connection.GetQuery(deleteBuilder))
            {
                query.BindParam("component", value.ID);
                query.ExecuteNoData();
            }

            using (ModifyEntityQuery query = connection.GetDeleteEntityQuery(typeof(DbComponent)))
                query.Execute(value);
        }

        public static int GetComponentsCount(SqlDbConnection connection)
        {
            using (SelectEntitiesCountQuery reader = connection.GetSelectEntitiesCountQuery(typeof(DbComponent)))
            {
                reader.Execute();
                return reader.RowCount;
            }
        }

        public static EntityCollection<DbComponent> ReadComponents(SqlDbConnection connection, int limit, int offset)
        {
            using (SelectEntitiesQuery reader = connection.GetSelectEntitiesQuery(typeof(DbComponent)))
            {
                reader.AddOrderBy(nameof(DbComponent.Name));
                reader.Limit = limit;
                reader.Skip = offset;
                reader.Execute();
                return reader.ReadAll<EntityCollection<DbComponent>, DbComponent>();
            }
        }

        public static DbLanguage NewLanguage()
        {
            return new DbLanguage();
        }

        public static DbLanguage ReadLanguage(SqlDbConnection connection, int id)
        {
            using (SelectEntitiesQuery query = connection.GetSelectEntitiesQuery(typeof(DbLanguage)))
            {
                query.AddWhereFilter(nameof(DbLanguage.ID), CmpOp.Eq, id);
                query.Execute();
                return query.ReadOne<DbLanguage>();
            }
        }


        public static void SaveLanguage(SqlDbConnection connection, DbLanguage value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using (ModifyEntityQuery query = value.ID < 1 ? connection.GetInsertEntityQuery(typeof(DbLanguage)) : connection.GetUpdateEntityQuery(typeof(DbLanguage)))
                query.Execute(value);
        }


        public static void DeleteLanguage(SqlDbConnection connection, DbLanguage value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            DeleteEntityQueryBuilder deleteBuilder = new DeleteEntityQueryBuilder(typeof(DbMessageText), connection);
            deleteBuilder.AddWhereFilter(nameof(DbMessageText.Language), CmpOp.Eq, "language");

            using (SqlDbQuery query = connection.GetQuery(deleteBuilder))
            {
                query.BindParam("language", value.ID);
                query.ExecuteNoData();
            }


            using (ModifyEntityQuery query = connection.GetDeleteEntityQuery(typeof(DbLanguage)))
                query.Execute(value);
        }

        public static int GetLanguagesCount(SqlDbConnection connection)
        {
            using (SelectEntitiesCountQuery reader = connection.GetSelectEntitiesCountQuery(typeof(DbLanguage)))
            {
                reader.Execute();
                return reader.RowCount;
            }
        }

        public static EntityCollection<DbLanguage> ReadLanguages(SqlDbConnection connection, int limit, int offset)
        {
            using (SelectEntitiesQuery reader = connection.GetSelectEntitiesQuery(typeof(DbLanguage)))
            {
                reader.AddOrderBy(nameof(DbLanguage.Name));
                reader.Limit = limit;
                reader.Skip = offset;
                reader.Execute();
                return reader.ReadAll<EntityCollection<DbLanguage>, DbLanguage>();
            }
        }

        public static DbMessage NewMessage()
        {
            return new DbMessage();
        }

        public static DbMessage ReadMessage(SqlDbConnection connection, int id)
        {
            using (SelectEntitiesQuery query = connection.GetSelectEntitiesQuery(typeof(DbMessage)))
            {
                query.AddWhereFilter(nameof(DbMessage.ID), CmpOp.Eq, id);
                query.Execute();
                return query.ReadOne<DbMessage>();
            }
        }


        public static void SaveMessage(SqlDbConnection connection, DbMessage value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using (ModifyEntityQuery query = value.ID < 1 ? connection.GetInsertEntityQuery(typeof(DbMessage)) : connection.GetUpdateEntityQuery(typeof(DbMessage)))
                query.Execute(value);
        }


        public static void DeleteMessage(SqlDbConnection connection, DbMessage value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            DeleteEntityQueryBuilder deleteBuilder = new DeleteEntityQueryBuilder(typeof(DbMessageText), connection);
            deleteBuilder.AddWhereFilter(nameof(DbMessageText.Message), CmpOp.Eq, "message");

            using (SqlDbQuery query = connection.GetQuery(deleteBuilder))
            {
                query.BindParam("message", value.ID);
                query.ExecuteNoData();
            }

            using (ModifyEntityQuery query = connection.GetDeleteEntityQuery(typeof(DbMessage)))
                query.Execute(value);
        }

        private static void BindMessagesFilter(SelectEntitiesQueryBase query, DbComponent component)
        {
            if (component != null)
                query.AddWhereFilter(nameof(DbMessage.Component), CmpOp.Eq, component.ID);
        }

        public static int GetMessagesCount(SqlDbConnection connection, DbComponent component)
        {
            using (SelectEntitiesCountQuery reader = connection.GetSelectEntitiesCountQuery(typeof(DbMessage)))
            {
                BindMessagesFilter(reader, component);
                reader.Execute();
                return reader.RowCount;
            }
        }

        public static EntityCollection<DbMessage> ReadMessages(SqlDbConnection connection, DbComponent component, int limit, int offset)
        {
            using (SelectEntitiesQuery reader = connection.GetSelectEntitiesQuery(typeof(DbMessage)))
            {
                BindMessagesFilter(reader, component);
                if (component == null)
                    reader.AddOrderBy(typeof(DbComponent), nameof(DbComponent.Name));
                reader.AddOrderBy(nameof(DbMessage.Name));
                reader.Limit = limit;
                reader.Skip = offset;
                reader.Execute();
                return reader.ReadAll<EntityCollection<DbMessage>, DbMessage>();
            }
        }

        public static DbMessageText NewMessageText()
        {
            return new DbMessageText();
        }

        public static DbMessageText ReadMessageText(SqlDbConnection connection, int id)
        {
            using (SelectEntitiesQuery query = connection.GetSelectEntitiesQuery(typeof(DbMessageText)))
            {
                query.AddWhereFilter(nameof(DbMessageText.ID), CmpOp.Eq, id);
                query.Execute();
                return query.ReadOne<DbMessageText>();
            }
        }


        public static void SaveMessageText(SqlDbConnection connection, DbMessageText value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using (ModifyEntityQuery query = value.ID < 1 ? connection.GetInsertEntityQuery(typeof(DbMessageText)) : connection.GetUpdateEntityQuery(typeof(DbMessageText)))
                query.Execute(value);
        }


        public static void DeleteMessageText(SqlDbConnection connection, DbMessageText value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            using (ModifyEntityQuery query = connection.GetDeleteEntityQuery(typeof(DbMessageText)))
                query.Execute(value);
        }

        private static void BindMessageTextsFilter(SelectEntitiesQueryBase query, DbComponent component, DbMessage message, DbLanguage language)
        {
            if (component != null)
                query.AddWhereFilter(typeof(DbMessage), nameof(DbMessage.Component), CmpOp.Eq, component);
            if (message != null)
                query.AddWhereFilter(typeof(DbMessageText), nameof(DbMessageText.Message), CmpOp.Eq, message);
            if (language != null)
                query.AddWhereFilter(typeof(DbMessageText), nameof(DbMessageText.Language), CmpOp.Eq, language);
        }

        public static int GetMessageTextsCount(SqlDbConnection connection, DbComponent component, DbMessage message, DbLanguage language)
        {
            using (SelectEntitiesCountQuery reader = connection.GetSelectEntitiesCountQuery(typeof(DbMessageText)))
            {
                BindMessageTextsFilter(reader, component, message, language);
                reader.Execute();
                return reader.RowCount;
            }
        }

        public static EntityCollection<DbMessageText> ReadMessageTexts(SqlDbConnection connection, DbComponent component, DbMessage message, DbLanguage language, int limit, int offset)
        {
            using (SelectEntitiesQuery reader = connection.GetSelectEntitiesQuery(typeof(DbMessageText)))
            {
                BindMessageTextsFilter(reader, component, message, language);
                reader.AddOrderBy(typeof(DbComponent), nameof(DbComponent.Name));
                reader.AddOrderBy(typeof(DbLanguage), nameof(DbLanguage.Name));
                reader.Limit = limit;
                reader.Skip = offset;
                reader.Execute();
                return reader.ReadAll<EntityCollection<DbMessageText>, DbMessageText>();
            }
        }

        public static int[] GetComponentLanguages(SqlDbConnection connection, DbComponent component)
        {
            SelectEntityQueryBuilderBase builder1 = new SelectEntityQueryBuilderBase(typeof(DbMessage), connection);
            builder1.Distinct = true;
            builder1.AddToResultset(nameof(DbMessage.ID), "id");
            builder1.AddWhereFilter(typeof(DbMessage), nameof(DbMessage.Component), CmpOp.Eq, "component");

            SelectEntityQueryBuilderBase builder = new SelectEntityQueryBuilderBase(typeof(DbMessageText), connection);
            builder.Distinct = true;
            builder.AddToResultset(nameof(DbMessageText.Language), "language");
            builder.AddWhereFilter(nameof(DbMessageText.Message), CmpOp.In, builder1);

            using (SqlDbQuery query = connection.GetQuery(builder))
            {
                query.BindParam("component", component.ID);
                query.ExecuteReader();
                List<int> v = new List<int>();
                while (query.ReadNext())
                    v.Add(query.GetValue<int>(0));
                return v.ToArray();
            }
        }

        public static TextMessageBlock ReadMessageTextBlock(SqlDbConnection connection, DbComponent component, DbLanguage language)
        {
            TextMessageBlock block = new TextMessageBlock() {Component = component.Name, Language = language.Name};

            SelectEntityQueryBuilderBase builder = new SelectEntityQueryBuilderBase(typeof(DbMessageText), connection);
            builder.AddEntity(typeof(DbMessage));
            builder.AddToResultset(typeof(DbMessage), nameof(DbMessage.Name), "name");
            builder.AddToResultset(typeof(DbMessageText), nameof(DbMessageText.Value), "value");

            builder.AddWhereFilter(typeof(DbMessage), nameof(DbMessage.Component), CmpOp.Eq, "component");
            builder.AddWhereFilter(typeof(DbMessageText), nameof(DbMessageText.Language), CmpOp.Eq, "language");

            using (SqlDbQuery query = connection.GetQuery(builder))
            {
                query.BindParam("component", component.ID);
                query.BindParam("language", language.ID);
                query.ExecuteReader();
                while (query.ReadNext())
                    block.Add(new TextMessage() {Name = query.GetValue<string>(0), Value = query.GetValue<string>(1)});
                return block;
            }

        }

    }

}
