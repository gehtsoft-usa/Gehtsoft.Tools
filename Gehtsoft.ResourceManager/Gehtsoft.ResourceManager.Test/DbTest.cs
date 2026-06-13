using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.EF.Db.SqlDb;
using Gehtsoft.EF.Db.SqliteDb;
using Gehtsoft.ResourceManager.Db;
using Xunit;

// These tests mutate global static ResourceManager / TextMessageLoader state and were
// written for the sequential NUnitLite runner; keep them sequential under xUnit.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Gehtsoft.ResourceManager.Test
{
    public class DbTest
    {
        [Fact]
        public static void Test()
        {
            DbComponent component1, component2;
            DbLanguage language1, language2;
            DbMessage message1, message2;
            DbMessageText text1, text2;


            using (SqlDbConnection connection = SqliteDbConnectionFactory.Create("Data Source=:memory:"))
            {
                MessagesDao.CreateTables(connection);

                component1 = new DbComponent() { Name = "component1" };
                component2 = new DbComponent() { Name = "component2" };

                MessagesDao.SaveComponent(connection, component1);
                MessagesDao.SaveComponent(connection, component2);

                language1 = new DbLanguage() {Name = "en" };
                language2 = new DbLanguage() { Name = "ru" };

                MessagesDao.SaveLanguage(connection, language1);
                MessagesDao.SaveLanguage(connection, language2);

                for (int i = 0; i < 10; i++)
                {
                    message1 = new DbMessage() {Component = component1, Name = $"c1message{i}"};
                    MessagesDao.SaveMessage(connection, message1);

                    text1 = new DbMessageText() { Message = message1, Language = language1, Value = message1.Name + "en" };
                    text2 = new DbMessageText() { Message = message1, Language = language2, Value = message1.Name + "ru" };
                    MessagesDao.SaveMessageText(connection, text1);
                    MessagesDao.SaveMessageText(connection, text2);

                    if (i < 5)
                    {
                        message2 = new DbMessage() { Component = component2, Name = $"c2message{i}" };
                        MessagesDao.SaveMessage(connection, message2);
                        text1 = new DbMessageText() { Message = message2, Language = language1, Value = message2.Name + "default" };
                        MessagesDao.SaveMessageText(connection, text1);
                    }
                }

                int[] languages = MessagesDao.GetComponentLanguages(connection, component1);
                Assert.Equal(2, languages.Length);
                languages = MessagesDao.GetComponentLanguages(connection, component2);
                Assert.Equal(language1.ID, Assert.Single(languages));

                TextMessageBlock block = MessagesDao.ReadMessageTextBlock(connection, component1, language1);
                Assert.Equal(10, block.Count);
                Assert.Equal("component1", block.Component);
                Assert.Equal("en", block.Language);
                foreach (TextMessage message in block)
                    Assert.Equal(message.Name + "en", message.Value);

                block = MessagesDao.ReadMessageTextBlock(connection, component1, language2);

                Assert.Equal(10, block.Count);
                Assert.Equal("component1", block.Component);
                Assert.Equal("ru", block.Language);
                foreach (TextMessage message in block)
                    Assert.Equal(message.Name + "ru", message.Value);

                block = MessagesDao.ReadMessageTextBlock(connection, component2, language1);

                Assert.Equal(5, block.Count);
                Assert.Equal("component2", block.Component);
                Assert.Equal("en", block.Language);
                foreach (TextMessage message in block)
                    Assert.Equal(message.Name + "default", message.Value);

                block = MessagesDao.ReadMessageTextBlock(connection, component2, language2);
                Assert.Equal(0, block.Count);

                MessagesDao.DeleteComponent(connection, component2);

                Assert.Null(MessagesDao.ReadComponent(connection, component2.ID));
                Assert.Equal(0, MessagesDao.GetMessagesCount(connection, component2));
                Assert.Equal(0, MessagesDao.GetMessageTextsCount(connection, component2, null, null));

                TextMessageBlock[] blocks = TextMessageLoaderDb.LoadDb("component1", connection);
                Assert.NotNull(blocks);
                Assert.Equal(2, blocks.Length);


                MessagesDao.DropTables(connection);
            }
        }
    }
}
