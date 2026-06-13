using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gehtsoft.Tools.IoC;
using Gehtsoft.Tools.IoC.Attributes;
using Xunit;

namespace Gehtsoft.Tools.UnitTest
{
    public class IoCTest
    {
        public interface ITestInterface<T>
        {
            bool Do(T item);
        }

        public class TestInterfaceImplementation1 : ITestInterface<int>
        {
            public bool Do(int item) => item != 0;
        }

        public class TestInterfaceImplementation2 : ITestInterface<string>
        {
            public bool Do(string item) => string.IsNullOrEmpty(item);
        }

        public interface IServiceInterface<T>
        {
            bool Done { get; }
        }

        public class Service<T> : IServiceInterface<T>
        {
            public bool Done { get; }
            
            public Service(ITestInterface<T> testInterface, T value)
            {
                Assert.NotNull(testInterface);
                Done = testInterface.Do(value);
            }
        }

        public class DefaultConstructorClass
        {
            [Inject]
            private DateTime mDateTime = default;   // assigned by IoC injection at runtime

            public DateTime DateTimeValue => mDateTime;

            [Inject]
            public string StringValue { get; private set; }

            public DefaultConstructorClass()
            {

            }
        }

        public class MultiConstructorClass
        {
            public double v { get; private set; } = -1;

            public MultiConstructorClass()
            {

            }

            public MultiConstructorClass(int x)
            {
                v = (double) x;
            }

            [IoCConstructor]
            public MultiConstructorClass(double x)
            {
                v = x;
            }
        }

        [Fact]
        public void TestConstructors()
        {
            IoCFactory factory = new IoCFactory();
            factory.AddSingleton<ITestInterface<int>, TestInterfaceImplementation1>();
            factory.AddSingleton<ITestInterface<string>, TestInterfaceImplementation2>();
            factory.Add<IServiceInterface<int>, Service<int>>();
            factory.Add<IServiceInterface<string>, Service<string>>();
            factory.AddSingleton<string>("12345");
            DateTime dt = DateTime.Now;
            factory.AddSingleton<DateTime>(dt);

            object o1, o2;

            o1 = factory.GetService<ITestInterface<int>>();
            o2 = factory.GetService<ITestInterface<int>>();
            Assert.NotNull(o1);
            Assert.NotNull(o2);
            Assert.True(o1 is ITestInterface<int>);
            Assert.True(ReferenceEquals(o1, o2));


            o1 = factory.GetService<IServiceInterface<int>>((int)0);
            o2 = factory.GetService<IServiceInterface<int>>((int)0);
            Assert.NotNull(o1);
            Assert.NotNull(o2);
            Assert.True(o1 is IServiceInterface<int>);
            Assert.False(ReferenceEquals(o1, o2));

            IServiceInterface<int> is1 = factory.GetService<IServiceInterface<int>>(0);
            Assert.False(is1.Done);
            is1 = factory.GetService<IServiceInterface<int>>(0);
            Assert.False(is1.Done);
            is1 = factory.GetService<IServiceInterface<int>>(10);
            Assert.True(is1.Done);

            DefaultConstructorClass dcf = factory.GetService<DefaultConstructorClass>();
            Assert.NotNull(dcf);
            #if NET45
            Assert.Equal(dt, dcf.DateTimeValue);
            Assert.Equal("12345", dcf.StringValue);
            #endif

            MultiConstructorClass mcf = factory.GetService<MultiConstructorClass>(1.5);
            Assert.NotNull(mcf);
            Assert.Equal(1.5, mcf.v);
        }
    }
}
