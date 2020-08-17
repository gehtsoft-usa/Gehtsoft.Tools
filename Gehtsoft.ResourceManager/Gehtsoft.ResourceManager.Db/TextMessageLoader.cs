using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Gehtsoft.EF.Db.SqlDb;
using Gehtsoft.ResourceManager.Db;

namespace Gehtsoft.ResourceManager
{
    public static class TextMessageLoaderDb
    {
        public static TextMessageBlock[] LoadDb(string componentName, SqlDbConnection connection)
        {
            List<TextMessageBlock> blocks = new List<TextMessageBlock>();
            DbComponent component = MessagesDao.FindComponent(connection, componentName);
            if (component != null)
            {
                int[] languages = MessagesDao.GetComponentLanguages(connection, component);

                foreach (int languageID in languages)
                {
                    DbLanguage language = MessagesDao.ReadLanguage(connection, languageID);
                    TextMessageBlock block = MessagesDao.ReadMessageTextBlock(connection, component, language);
                    if (block != null && block.Count > 0)
                        blocks.Add(block);
                }
            }
            return blocks.ToArray();
        }

    }
}
