using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
    [TestFixture]
    public class TypeUtilsTest
    {
        [Test]
        public void TestFindIniterface()
        {
            List<int> list = new List<int>();
            Dictionary<int, string> dictionary = new Dictionary<int, string>();

            Assert.AreEqual(typeof(IEnumerable), list.GetType().ExtractImplementation(typeof(IEnumerable)));
            Assert.IsNull(list.GetType().ExtractImplementation(typeof(IDictionary)));
            Assert.AreEqual(typeof(IEnumerable), dictionary.GetType().ExtractImplementation(typeof(IEnumerable)));
            Assert.AreEqual(typeof(IDictionary), dictionary.GetType().ExtractImplementation(typeof(IDictionary)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractImplementation(typeof(IEnumerable<>)));

            Assert.AreEqual(typeof(IEnumerable<int>), list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(int)));
            Assert.IsNull(list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(string)));
            Assert.AreEqual(typeof(IDictionary<int, string>), dictionary.GetType().ExtractGenericImplementation(typeof(IDictionary<,>), typeof(int), typeof(string)));
            Assert.IsNull(dictionary.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(string), typeof(int)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractGenericImplementation(typeof(IEnumerable)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>)));
        }

        [Test]
        public void TestLoadAssembly()
        {
            //assembly located in app path
            Assembly assembly = AssemblyUtils.FindAssembly("Gehtsoft.Tools.CommandLine.dll");
            Assert.IsNotNull(assembly);
            Assert.IsTrue(string.Compare("Gehtsoft.Tools.CommandLine", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            //assembly existed in call stack
            assembly = AssemblyUtils.FindAssembly("nunit.framework.dll");
            Assert.IsNotNull(assembly);
            Assert.IsTrue(string.Compare("nunit.framework", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            //assembly existed in nuget and referenced by the current application
            assembly = AssemblyUtils.FindAssembly("Newtonsoft.Json.dll");
            Assert.IsNotNull(assembly);
            Assert.IsTrue(string.Compare("Newtonsoft.Json", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            assembly = AssemblyUtils.FindAssembly("Not.Existed.Or.Referenced.dll");
            Assert.IsNull(assembly);
        }
    }
}
