using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.TypeUtils;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class TypeUtilsTest
    {
        [Fact]
        public void TestFindIniterface()
        {
            List<int> list = new List<int>();
            Dictionary<int, string> dictionary = new Dictionary<int, string>();

            Assert.Equal(typeof(IEnumerable), list.GetType().ExtractImplementation(typeof(IEnumerable)));
            Assert.Null(list.GetType().ExtractImplementation(typeof(IDictionary)));
            Assert.Equal(typeof(IEnumerable), dictionary.GetType().ExtractImplementation(typeof(IEnumerable)));
            Assert.Equal(typeof(IDictionary), dictionary.GetType().ExtractImplementation(typeof(IDictionary)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractImplementation(typeof(IEnumerable<>)));

            Assert.Equal(typeof(IEnumerable<int>), list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(int)));
            Assert.Null(list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(string)));
            Assert.Equal(typeof(IDictionary<int, string>), dictionary.GetType().ExtractGenericImplementation(typeof(IDictionary<,>), typeof(int), typeof(string)));
            Assert.Null(dictionary.GetType().ExtractGenericImplementation(typeof(IEnumerable<>), typeof(string), typeof(int)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractGenericImplementation(typeof(IEnumerable)));
            Assert.Throws<ArgumentException>(() => list.GetType().ExtractGenericImplementation(typeof(IEnumerable<>)));
        }

        [Fact]
        public void TestLoadAssembly()
        {
            //assembly located in app path
            Assembly assembly = AssemblyUtils.FindAssembly("Gehtsoft.Tools.CommandLine.dll");
            Assert.NotNull(assembly);
            Assert.True(string.Compare("Gehtsoft.Tools.CommandLine", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            //assembly existed in call stack
            assembly = AssemblyUtils.FindAssembly("xunit.v3.core.dll");
            Assert.NotNull(assembly);
            Assert.True(string.Compare("xunit.v3.core", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            //assembly existed in nuget and referenced by the current application
            assembly = AssemblyUtils.FindAssembly("Newtonsoft.Json.dll");
            Assert.NotNull(assembly);
            Assert.True(string.Compare("Newtonsoft.Json", assembly.GetName().Name, StringComparison.OrdinalIgnoreCase) == 0);
            assembly = AssemblyUtils.FindAssembly("Not.Existed.Or.Referenced.dll");
            Assert.Null(assembly);
        }
    }
}
