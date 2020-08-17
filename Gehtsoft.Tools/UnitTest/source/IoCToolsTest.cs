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
using NUnit.Framework;

namespace Gehtsoft.Tools.UnitTest
{
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
                Assert.IsNotNull(test);
                test.f1 = true;
                Assert.AreEqual(1, p1);
                Assert.AreEqual(2.0, p2);
                Assert.AreEqual("3", p3);
            }
        }

        [Test]
        public void ClassFilterTest()
        {
            IList<Type> types;
            Profile profile = new Profile();    //<-- force profiles to be loaded
            
            types = TypeFinder.InAllAssemblies().GetTypes().ToList();

            Assert.AreEqual(1, types.Count(t => t == typeof(IoCToolsTest)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestAttributeAttribute)));
            Assert.AreEqual(1, types.Count(t => t == typeof(Profile)));
            Assert.AreEqual(1, types.Count(t => t == typeof(ITestInterface1)));

            //find by attribute
            types = TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestFixtureAttribute>().GetTypes().ToList();
            Assert.AreEqual(1, types.Count(t => t == typeof(IoCToolsTest)));
            Assert.AreEqual(1, types.Count(t => t == typeof(ExpressionUtilTest)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestAttributeAttribute)));
            Assert.AreEqual(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().GetTypes().ToList();
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass3)));
            Assert.AreEqual(1, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().WhichIsClass().GetTypes().ToList();
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass3)));
            Assert.AreEqual(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<>)).GetTypes().ToList();
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass3)));
            Assert.AreEqual(1, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<int>)).GetTypes().ToList();
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass3)));
            Assert.AreEqual(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<Attribute>)).GetTypes().ToList();
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass3)));
            Assert.AreEqual(0, types.Count(t => t == typeof(ITestInterface1)));

            types = TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable)).WhichHasAttribute<TestAttributeAttribute>().GetTypes().ToList();
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass1)));
            Assert.AreEqual(1, types.Count(t => t == typeof(TestClass2)));
            Assert.AreEqual(0, types.Count(t => t == typeof(TestClass3)));

            types = TypeFinder.NearClass<IoCToolsTest>().Which(type => type.Name == "TestAttributeAttribute").GetTypes().ToList();
            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(1, types.Count(t => t == typeof(TestAttributeAttribute)));

            int count = 0;
            TypeFinder.NearClass<IoCToolsTest>().ForAll(type => count += (type.Name == "TestAttributeAttribute" ? 1 : 0));
            Assert.AreEqual(1, count);

            f1 = false;
            TypeFinder.NearClass<IoCToolsTest>().Which(type => type.Name == nameof(TestCallClass)).InvokeForAll(nameof(TestCallClass.Do), null, new object[] {2.0, "3", 1, this});
            Assert.IsTrue(f1);

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

        [Test]
        public void ClassRegisterAction()
        {
            TestRegisry registry = new TestRegisry();
            TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestAttributeAttribute>().RegisterAll(registry);
            Assert.AreEqual(1, registry.Registry.Count);
            Assert.IsTrue(registry.Contains(typeof(TestClass2), typeof(TestClass2), RegistryMode.CreateEveryTime));

            registry.Clear();
            TypeFinder.NearClass<IoCToolsTest>().WhichImplements<IEnumerable>().WhichIsClass().RegisterAll(registry, RegistryMode.Singleton, typeof(IEnumerable));
            Assert.IsTrue(registry.Contains(typeof(IEnumerable), typeof(TestClass1), RegistryMode.Singleton));
            Assert.IsTrue(registry.Contains(typeof(IEnumerable), typeof(TestClass2), RegistryMode.Singleton));
            Assert.IsTrue(registry.Contains(typeof(IEnumerable), typeof(TestClass3), RegistryMode.Singleton));
            Assert.IsFalse(registry.Contains(typeof(ITestInterface1)));

            registry.Clear();
            TypeFinder.NearClass<IoCToolsTest>().WhichImplements(typeof(IEnumerable<>)).WhichIsClass().RegisterAll(registry, RegistryMode.Singleton, typeof(IEnumerable<>));
            Assert.IsTrue(registry.Contains(typeof(IEnumerable<int>), typeof(TestClass1), RegistryMode.Singleton));
            Assert.IsFalse(registry.Contains(typeof(TestClass2)));
            Assert.IsTrue(registry.Contains(typeof(IEnumerable<Attribute>), typeof(TestClass3), RegistryMode.Singleton));
            Assert.IsFalse(registry.Contains(typeof(ITestInterface1)));
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
                Assert.IsNotNull(test);
                Assert.IsNotNull(arg);
                test.f1 = true;
                
            }
        }

        [TestInvoke]
        public class TestInvoke2
        {
            public void TestMethod(TestClass2 arg, IoCToolsTest test)
            {
                Assert.IsNotNull(test);
                Assert.IsNotNull(arg);
                test.f2 = true;
                
            }
        }

        public bool f1, f2;

        [Test]
        public void ClassInvokeAction()
        {
            f1 = f2 = false;
            TypeFinder.NearClass<IoCToolsTest>().WhichHasAttribute<TestInvokeAttribute>().InvokeForAll("TestMethod", new InvokeClassFactory(this));
            Assert.IsTrue(f1 && f2);
        }

    }
}
