using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.ConfigurationProfile;
using Gehtsoft.Tools.IoC;
using Gehtsoft.Tools.IoC.Tools;
using Gehtsoft.Tools.TypeUtils;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    // Marker attribute used by ClassFilterTest to exercise TypeFinder.WhichHasAttribute.
    // Replaces NUnit's [TestFixture], which was used as the marker before the xUnit migration.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestFixtureAttribute : Attribute
    {
    }

    [TestFixture]
    public class IoCToolsTest
    {
        [AttributeUsage(AttributeTargets.Class)]
        public class TestAttributeAttribute : Attribute
        {

        }

        public interface ITestInterface1 : IEnumerable<double>
        {

        }

        public class TestClass1 : IEnumerable<int>
        {
            public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [@TestAttributeAttribute]
        public class TestClass2 : IEnumerable
        {
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        public class TestClass3 : IEnumerable<Attribute>
        {
            public IEnumerator<Attribute> GetEnumerator() => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class TestCallClass
        {

            public static void Do(int p1, double p2, string p3, IoCToolsTest test)
            {
                Assert.NotNull(test);
                test.f1 = true;
                Assert.Equal(1, p1);
                Assert.Equal(2.0, p2);
                Assert.Equal("3", p3);
            }
        }

        [Fact]
        public void ClassFilterTest()
        {
            IList<Type> types;
            Profile profile = new Profile();    //<-- force profiles to be loaded
            
            types = TypeFinder.InAllAssemblies().GetTypes().ToList();

            Assert.Equal(1, types.Count(t => t == typeof(IoCToolsTest)));
            Assert.Equal(1, types.Count(t => t == typeof(TestAttributeAttribute)));
            Assert.Equal(1, types.Count(t => t == typeof(Profile)));
            Assert.Equal(1, types.Count(t => t == typeof(ITestInterface1)));

            //find by attribute
            types = TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestFixtureAttribute>().GetTypes().ToList();
            Assert.Equal(1, types.Count(t => t == typeof(IoCToolsTest)));
            Assert.Equal(1, types.Count(t => t == typeof(ExpressionUtilTest)));
            Assert.Equal(0, types.Count(t => t == typeof(TestAttributeAttribute)));
            Assert.Equal(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().GetTypes().ToList();
            Assert.Equal(1, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass3)));
            Assert.Equal(1, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().WhichIsClass().GetTypes().ToList();
            Assert.Equal(1, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass3)));
            Assert.Equal(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<>)).GetTypes().ToList();
            Assert.Equal(1, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(0, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass3)));
            Assert.Equal(1, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<int>)).GetTypes().ToList();
            Assert.Equal(1, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(0, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(0, types.Count(t => t == typeof(TestClass3)));
            Assert.Equal(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<Attribute>)).GetTypes().ToList();
            Assert.Equal(0, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(0, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass3)));
            Assert.Equal(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable)).WhichHasAttribute<TestAttributeAttribute>().GetTypes().ToList();
            Assert.Equal(0, types.Count(t => t == typeof(TestClass1)));
            Assert.Equal(1, types.Count(t => t == typeof(TestClass2)));
            Assert.Equal(0, types.Count(t => t == typeof(TestClass3)));

            types = TypeFinder.NearClass<IoCToolsTest>().Which(type => type.Name == "TestAttributeAttribute").GetTypes().ToList();
            Assert.Single(types);
            Assert.Equal(1, types.Count(t => t == typeof(TestAttributeAttribute)));

            int count = 0;
            TypeFinder.NearClass<IoCToolsTest>().ForAll(type => count += (type.Name == "TestAttributeAttribute" ? 1 : 0));
            Assert.Equal(1, count);

            f1 = false;
            TypeFinder.NearClass<IoCToolsTest>().Which(type => type.Name == nameof(TestCallClass)).InvokeForAll(nameof(TestCallClass.Do), null, new object[] {2.0, "3", 1, this});
            Assert.True(f1);

            List<Tuple<Type, Type>> registry = new List<Tuple<Type, Type>>();

            TypeFinder.NearClass<IoCToolsTest>()
                .WhichImplements(typeof(IEnumerable<>))
                .RegisterAll(typeof(IEnumerable<>), (type, type1) => 
                { 
                    registry.Add(new Tuple<Type, Type>(type, type1));
                });

            ;
        }

        public class TestRegisry : IClassRegistry
        {
            public List<Tuple<Type, Type, RegistryMode>> Registry { get; } = new List<Tuple<Type, Type, RegistryMode>>();

            public void Add(Type registryType, Type implementationType, RegistryMode mode) => Registry.Add(new Tuple<Type, Type, RegistryMode>(registryType, implementationType, mode));

            public bool Contains(Type registryType, Type implementationType, RegistryMode mode) => Registry.Any(record => record.Item1 == registryType && record.Item2 == implementationType && record.Item3 == mode);

            public bool Contains(Type implementationType) => Registry.Any(record => record.Item2 == implementationType);

            public void Clear() => Registry.Clear();
        }

        [Fact]
        public void ClassRegisterAction()
        {
            TestRegisry registry = new TestRegisry();
            TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestAttributeAttribute>().RegisterAll(registry);
            Assert.Single(registry.Registry);
            Assert.True(registry.Contains(typeof(TestClass2), typeof(TestClass2), RegistryMode.CreateEveryTime));

            registry.Clear();
            TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().WhichIsClass().RegisterAll(registry, RegistryMode.Singleton, typeof(IEnumerable));
            Assert.True(registry.Contains(typeof(IEnumerable), typeof(TestClass1), RegistryMode.Singleton));
            Assert.True(registry.Contains(typeof(IEnumerable), typeof(TestClass2), RegistryMode.Singleton));
            Assert.True(registry.Contains(typeof(IEnumerable), typeof(TestClass3), RegistryMode.Singleton));
            Assert.False(registry.Contains(typeof(ITestInterface1)));

            registry.Clear();
            TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<>)).WhichIsClass().RegisterAll(registry, RegistryMode.Singleton, typeof(IEnumerable<>));
            Assert.True(registry.Contains(typeof(IEnumerable<int>), typeof(TestClass1), RegistryMode.Singleton));
            Assert.False(registry.Contains(typeof(TestClass2)));
            Assert.True(registry.Contains(typeof(IEnumerable<Attribute>), typeof(TestClass3), RegistryMode.Singleton));
            Assert.False(registry.Contains(typeof(ITestInterface1)));
        }

        public class TestArgumentClass1
        {
            public TestArgumentClass1()
            {

            }
        }

        public class TestArgumentClass2
        {
            public TestArgumentClass2()
            {

            }
        }
        
        public class InvokeClassFactory : IServiceProvider
        {
            private IoCToolsTest mSignletone;

            public InvokeClassFactory(IoCToolsTest st)
            {
                mSignletone = st;
            }

            public object GetService(Type type)
            {
                if (type == typeof(IoCToolsTest))
                    return mSignletone;

                return Activator.CreateInstance(type);
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class TestInvokeAttribute : Attribute
        {

        }

        [TestInvoke]
        public class TestInvoke1
        {
            public static void TestMethod(IoCToolsTest test, TestClass1 arg)
            {
                Assert.NotNull(test);
                Assert.NotNull(arg);
                test.f1 = true;
                
            }
        }

        [TestInvoke]
        public class TestInvoke2
        {
            public void TestMethod(TestClass2 arg, IoCToolsTest test)
            {
                Assert.NotNull(test);
                Assert.NotNull(arg);
                test.f2 = true;
                
            }
        }

        public bool f1, f2;

        [Fact]
        public void ClassInvokeAction()
        {
            f1 = f2 = false;
            TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestInvokeAttribute>().InvokeForAll("TestMethod", new InvokeClassFactory(this));
            Assert.True(f1 && f2);
        }

    }
}
