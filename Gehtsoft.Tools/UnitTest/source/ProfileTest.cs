using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using System.Runtime.Serialization;
using Gehtsoft.Tools.ConfigurationProfile;
using Gehtsoft.Tools.Crypto;
using Gehtsoft.Tools.FileUtils;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class ProfileTest
    {
        [Test]
        public void TestProfileStrings()
        {
            Profile profile = new Profile();

            profile.Set("section1", "key1", "value1");
            profile.Set("section1", "key2", "value2");
            profile.Set("section1", "key3", "value3");
            profile.Set("section2", "key1", "value4");

            Assert.IsTrue(profile.HasSection("section1"));
            Assert.IsTrue(profile.HasSection("section2"));
            Assert.IsFalse(profile.HasSection("section3"));
            Assert.IsFalse(profile.HasSection("section10"));
            Assert.IsTrue(profile.HasValue("section1", "key3"));
            Assert.IsTrue(profile.HasValue("section2", "key1"));
            Assert.IsFalse(profile.HasValue("section3", "key1"));
            Assert.IsFalse(profile.HasValue("section3", "key1"));

            Assert.AreEqual(profile.Get("section1", "key3"), "value3");
            Assert.AreEqual(profile.Get<string>("section1", "key3", null), "value3");
            Assert.AreEqual(profile.Get("section2", "key1"), "value4");
            Assert.IsNull(profile.Get("section1", "key4"));
            profile.Set("section1", "key4", "value4");
            Assert.IsNotNull(profile.Get("section1", "key4"));
            profile.RemoveKey("section1", "key4");
            Assert.IsNull(profile.Get("section1", "key4"));


            IEnumerable<string> en;
            en = profile.GetSections();
            int cc = 0;
            foreach (string s in en)
            {
                if (s == "section1")
                    cc++;
                else if (s == "section2")
                    cc++;
            }
            Assert.AreEqual(cc, 2);
            en = profile.GetKeys("section1");
             cc = 0;
            foreach (string s in en)
            {
                if (s == "key1")
                    cc++;
                else if (s == "key2")
                    cc++;
                else if (s == "key3")
                    cc++;
            }
            Assert.AreEqual(cc, 3);

            profile.RemoveSection("section1");
            Assert.IsFalse(profile.HasSection("section1"));
            Assert.IsTrue(profile.HasSection("section2"));
        }

        [Serializable]
        public class point : IEquatable<point>
        {
            public int X { get; set; }
            public int Y { get; set; }

            public point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public point()
            {

            }

            public bool Equals(point other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((point) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Y;
                }
            }
        }

        [Test]
        public void TestProfileTypes()
        {
            Profile profile = new Profile();

            DateTime now = DateTime.Now;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

            profile.Set("", "ikey", 10);
            profile.Set("", "dkey", 3.1415);
            profile.Set("", "bkey", true);
            profile.Set("", "tkey", now);

            Assert.AreEqual(profile.Get<int>("", "ikey"), 10);
            Assert.AreEqual(profile.Get<double>("", "dkey"), 3.1415);
            Assert.AreEqual(profile.Get<bool>("", "bkey"), true);
            Assert.AreEqual(profile.Get<DateTime>("", "tkey"), now);

            point pt1 = new point(10, 20);

            profile.Set<point>("", "okey", pt1);
            point pt2 = profile.Get<point>("", "okey");

            Assert.AreEqual(pt1, pt2);
        }

        [Test]
        public void TestProfileRw()
        {
            Profile profile1 = new Profile(), profile2;
            ProfileFactory.Encrypt = RCFourAlgorithm.Encode;
            ProfileFactory.Decrypt = RCFourAlgorithm.Decode;

            profile1.Set("section1", "key1", "value1");
            profile1.Set("section1", "key2", "value2");
            profile1.Set("section1", "key3", "value3");
            profile1.Set("section2", "key1", "value4");
            profile1.SetSecure("section2", "key2", "value5", "password");
            profile1.Set("section3", "key1", "v");
            profile1.Set("", "ikey0", 1);
            profile1.Set("", "ikey", 10);
            profile1.Set("", "dkey", 3.1415);
            profile1.Set("", "bkey", true);
            profile1.Set("", "tkey", DateTime.Now);

            ProfileLoader.SaveProfile("test", profile1);
            profile2 = ProfileLoader.LoadProfile("test");

            Assert.AreEqual(profile1, profile2);

            profile1.Set("section3", "key1", "value5");
            Assert.AreNotEqual(profile1, profile2);

            profile2.Set("section3", "key1", "value5");
            Assert.AreEqual(profile1, profile2);
            profile2.Set("section3", "key2", "value6");
            Assert.IsFalse((profile1 as object).Equals(profile2 as object));
            Assert.AreEqual("value5", profile2.GetSecure("section2", "key2", "password", ""));
            Assert.AreNotEqual("value5", profile2.GetSecure("section2", "key2", "wrongpasswordd", ""));
            Assert.AreNotEqual("value5", profile2.Get("section2", "key2", ""));
        }

        private bool mTestProfileFactoryReloaded = false;

        [Test]
        public void TestProfileFactory()
        {
            string fullName = Path.Combine(TypePathUtil.TypeFolder(typeof(ProfileTest)), "myfile.ini");
            if (File.Exists(fullName))
                File.Delete(fullName);
            ProfileFactory.Instance.Configure(fullName, true, true);
            Profile profile = ProfileFactory.Instance.Profile;
            ProfileFactory.Instance.ProfileChanged += () => mTestProfileFactoryReloaded = true;
            Assert.AreEqual(profile.SectionsCount, 0);
            mTestProfileFactoryReloaded = false;
            using (FileStream fs = new FileStream(fullName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(@"[section1]
                           key1 = value1");
                }
            }
            int wt = 0;
            while (!mTestProfileFactoryReloaded && wt < 20)
            {
                wt++;
                Thread.Sleep(50);
            }
            profile = ProfileFactory.Instance.Profile;
            Assert.IsTrue(profile.HasSection("section1"));
            Assert.IsTrue(profile.HasValue("section1", "key1"));

            profile.Set("section2", "key2", "value");
            Thread.Sleep(1100);

        }

        private const string TestProfile = "  [section1]\n  key1=value1\nkey2=value2\n[section2]   \nkey3=[value3]";

        [Test]
        public void TestProfileLoader()
        {
            string tfn = Path.GetTempPath() + Guid.NewGuid().ToString() + ".ini";
            try
            {
                File.WriteAllText(tfn, TestProfile);
                Profile profile = ProfileLoader.LoadProfile(tfn);
                Assert.AreEqual("value1", profile.Get<string>("section1", "key1"));
                Assert.AreEqual("value2", profile.Get<string>("section1", "key2"));
                Assert.AreEqual("value2", profile.Get<string>("Section1", "Key2"));
                Assert.AreEqual(null, profile.Get<string>("section1", "key3"));
                Assert.AreEqual("[value3]", profile.Get<string>("section2", "key3"));
            }
            finally
            {
                if (File.Exists(tfn))
                    File.Delete(tfn);
            }
        }
    }
}
