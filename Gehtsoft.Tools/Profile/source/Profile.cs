using System;
using System.Reflection;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Gehtsoft.Tools.ConfigurationProfile
{
    public class Profile : IEquatable<Profile>
    {
        public object Mutex { get; } = new object();
        public bool Changed { get; internal set; }
        public string Source { get; internal set; }

        class EquatableDictionary<T1, T2> : Dictionary<T1, T2>, IEquatable<EquatableDictionary<T1, T2>>
        {
            public EquatableDictionary() : base()
            {

            }

            public EquatableDictionary(IEqualityComparer<T1> comparer) : base(comparer)
            {
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((EquatableDictionary<T1, T2>) obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public bool Equals(EquatableDictionary<T1, T2> other)
            {
                if (other == null)
                    return false;

                bool all = true;
                foreach (T1 key in this.Keys)
                {
                    all &= other.ContainsKey(key);
                    if (!all)
                        break;
                    T2 v1, v2;
                    v1 = this[key];
                    v2 = other[key];
                    all &= v1.Equals(v2);
                }

                foreach (T1 key in other.Keys)
                    all &= this.ContainsKey(key);

                return all;
            }
        }

        private EquatableDictionary<string, EquatableDictionary<string, string>> mProfile = new EquatableDictionary<string, EquatableDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public bool HasValue(string section, string key)
        {
            EquatableDictionary<string, string> sectionDict;
            return mProfile.TryGetValue(section, out sectionDict) && sectionDict.ContainsKey(key);
        }
        public bool HasSection(string section)
        {
            return mProfile.ContainsKey(section);
        }

        public T Get<T>(string section, string key)
        {
            return Get<T>(section, key, default(T));
        }

        public T Get<T>(string section, string key, T defaultValue)
        {
            Type t = typeof(T);
            bool isValue;
#if NETCORE
            isValue = t.GetTypeInfo().IsValueType;
#else
            isValue = t.IsValueType;
#endif
            if (isValue || typeof(T) == typeof(string))
            {
                EquatableDictionary<string, string> sectionDict;
                if (mProfile.TryGetValue(section, out sectionDict))
                {
                    string value;
                    if (sectionDict.TryGetValue(key, out value))
                    {
                        try
                        {
                            return (T) Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            return defaultValue;
                        }
                    }
                    else
                        return defaultValue;
                }
                else
                    return defaultValue;
            }
            else
            {
                return Deserialize(section, key, defaultValue);
            }
        }

        public void Set<T>(string section, string key, T value)
        {

            Type t = typeof(T);
            bool isValue;
#if NETCORE
            isValue = t.GetTypeInfo().IsValueType;
#else
            isValue = t.IsValueType;
#endif
            if (isValue)
            {
                string svalue;
                svalue = (string) Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture);

                EquatableDictionary<string, string> sectionDict;
                if (!mProfile.TryGetValue(section, out sectionDict))
                {
                    sectionDict = new EquatableDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    mProfile[section] = sectionDict;
                }
                sectionDict[key] = svalue;
                Changed = true;
            }
            else
                Serialize(section, key, value);
        }

        public void SetBinary(string section, string key, byte[] arr)
        {
            Set(section, key, Convert.ToBase64String(arr));
        }

        public byte[] GetBinary(string section, string key)
        {
            string v = Get(section, key, null);
            if (v == null)
                return null;
            try
            {
                return Convert.FromBase64String(v);
            }
            catch (Exception )
            {
                return null;
            }
        }

        public string Get(string section, string key)
        {
            return Get(section, key, null);
        }

        public string Get(string section, string key, string defaultValue)
        {
            EquatableDictionary<string, string> sectionDict;
            if (mProfile.TryGetValue(section, out sectionDict))
            {
                string value;
                if (sectionDict.TryGetValue(key, out value))
                {
                    return value;
                }
                else
                    return defaultValue;
            }
            else
                return defaultValue;
        }

        public void SetSecure(string section, string key, string value, string password)
        {
            Set(section, key, ProfileFactory.Encrypt(value, password));
        }

        public string GetSecure(string section, string key, string password, string defaultValue)
        {
            string s = Get(section, key, null);
            if (s == null)
                return defaultValue;
            return ProfileFactory.Decrypt(s, password, defaultValue);
        }


        public void Set(string section, string key, string value)
        {
            EquatableDictionary<string, string> sectionDict;
            if (!mProfile.TryGetValue(section, out sectionDict))
            {
                sectionDict = new EquatableDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                mProfile[section] = sectionDict;
            }
            sectionDict[key] = value;
            Changed = true;
        }

        public IEnumerable<string> GetSections()
        {
            return mProfile.Keys;
        }

        public IEnumerable<string> GetKeys(string section)
        {
            EquatableDictionary<string, string> sectDict;
            if (mProfile.TryGetValue(section, out sectDict))
                return sectDict.Keys;
            else
                return null;
        }

        public void Serialize<T>(string section, string key, T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                byte[] ser = ms.ToArray();
                SetBinary(section, key, ser);
            }
        }

        public T Deserialize<T>(string section, string key)
        {
            return Deserialize<T>(section, key, default(T));
        }

        public T Deserialize<T>(string section, string key, T defaultT)
        {
            byte[] ser = GetBinary(section, key);
            if (ser == null)
                return defaultT;
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream(ser))
                {
                    T t = (T) formatter.Deserialize(ms);
                    return t;
                }
            }
            catch (Exception )
            {
                return defaultT;
            }
        }

        public int SectionsCount => mProfile.Keys.Count;

        public void RemoveKey(string section, string key)
        {
            EquatableDictionary<string, string> sectionDict;
            if (mProfile.TryGetValue(section, out sectionDict))
            {
                if (sectionDict.ContainsKey(key))
                    sectionDict.Remove(key);
            }
        }

        public void RemoveSection(string section)
        {
            if (mProfile.ContainsKey(section))
                mProfile.Remove(section);
        }

        public bool Equals(Profile other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(mProfile, other.mProfile);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Profile) obj);
        }
        public override int GetHashCode()
        {
            return (mProfile != null ? mProfile.GetHashCode() : 0);
        }
    }
}
