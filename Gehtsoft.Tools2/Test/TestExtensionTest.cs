using FluentAssertions;
using Gehtsoft.Tools2.Algorithm.DFA;
using Gehtsoft.Tools2.Extensions;
using Gehtsoft.Tools2.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class TypeExtensionTest
    {
        [Fact]
        public void FileName()
        {
            var cn = typeof(FastDFA).ContainerFileName();
            Path.IsPathRooted(cn).Should().BeTrue();
            cn.ToLower().Should().EndWith("gehtsoft.tools2.dll");
            File.Exists(cn).Should().BeTrue();
        }

        [Fact]
        public void TypeFolder()
        {
            var cn = typeof(FastDFA).TypeFolder();
            Path.IsPathRooted(cn).Should().BeTrue();
            Directory.Exists(cn).Should().BeTrue();
            Directory.EnumerateFiles(cn).Should().Contain(s => s.ToLower().EndsWith("gehtsoft.tools2.dll"));
        }

        public class MyList<T> : List<T>
        {
        }

        [Theory]
        [InlineData(typeof(MyList<int>), typeof(MyList<int>), typeof(MyList<int>), "same type")]
        [InlineData(typeof(MyList<int>), typeof(List<int>), typeof(List<int>), "parent type")]
        [InlineData(typeof(MyList<int>), typeof(List<>), typeof(List<int>), "parent generic type")]
        [InlineData(typeof(MyList<int>), typeof(ICollection<int>), typeof(ICollection<int>), "parent interface")]
        [InlineData(typeof(MyList<int>), typeof(ICollection<>), typeof(ICollection<int>), "generic interface")]
        [InlineData(typeof(MyList<int>), typeof(ICollection), typeof(ICollection), "simple interface")]
        public void ExactType_OK(Type type, Type baseType, Type result, string because)
        {
            type.ExtractImplementation(baseType).Should().Be(result, because);
        }

        [Theory]
        [InlineData(typeof(List<int>), typeof(MyList<int>))]
        [InlineData(typeof(List<int>), typeof(LinkedList<int>))]
        [InlineData(typeof(LinkedList<int>), typeof(IList<int>))]
        [InlineData(typeof(LinkedList<int>), typeof(IList<>))]
        [InlineData(typeof(LinkedList<int>), typeof(IDictionary))]
        public void ExactType_Fail(Type type, Type baseType)
        {
            ((Action)(() => type.ExtractImplementation(baseType))).Should().Throw<ArgumentException>();
        }
    }

    public class TypeFinderTest
    {
        [Fact]
        public void Everywhere()
        {
            var everywhere = TypeFinder.EveryWhere;
            everywhere.Should().Contain(typeof(List<>));
            everywhere.Should().Contain(typeof(TypeFinder));
            everywhere.Should().Contain(typeof(TypeFinderTest));
        }

        [Fact]
        public void NearType()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));
            locally.Should().NotContain(typeof(List<>));
            locally.Should().NotContain(typeof(TypeFinder));
            locally.Should().Contain(typeof(FastDFATest));
            locally.Should().Contain(typeof(TypeFinderTest));
        }

        [Serializable]
        public class TestClass1 : List<int>
        {
        }

        public class TestClass2 : Dictionary<int, string>
        {
        }

        public class TestClass3 : FastDFA
        {
            public TestClass3() : base(1, 1) { }
        }

        [Fact]
        public void WhichHaveAttribute()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            locally.WhichHaveAttribute<SerializableAttribute>().Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichImplements_Interface_1()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichImplements<IEnumerable>()).Should()
                .Contain(typeof(TestClass1))
                .And
                .Contain(typeof(TestClass2));
        }

        [Fact]
        public void WhichImplements_Interface_2()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichImplements<IDictionary>()).Should()
                .NotContain(typeof(TestClass1))
                .And
                .Contain(typeof(TestClass2));
        }

        [Fact]
        public void WhichImplements_Interface_3()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichImplements<IList>()).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichImplements_Interface_4()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichImplements<IList<int>>()).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichImplements_Interface_5()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichImplements(typeof(IList<>))).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichDerivedFrom_1()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichDerivedFrom(typeof(FastDFA))).Should()
                .Contain(typeof(TestClass3))
                .And
                .NotContain(typeof(TestClass1));
        }

        [Fact]
        public void WhichDerivedFrom_2()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichDerivedFrom(typeof(List<int>))).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichDerivedFrom_3()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichDerivedFrom(typeof(List<>))).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichDerivedFrom_4()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichDerivedFrom(typeof(IList<int>))).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        [Fact]
        public void WhichDerivedFrom_5()
        {
            var locally = TypeFinder.NearType(typeof(TypeFinderTest));

            (locally.WhichDerivedFrom(typeof(IList<>))).Should()
                .Contain(typeof(TestClass1))
                .And
                .NotContain(typeof(TestClass2));
        }

        public interface TestInterface<T>
        {
        }
        public class TestClass10 : TestInterface<int>
        {
        }
        public class TestClass11 : TestInterface<double>
        {
        }
        public class TestClass12 : TestInterface<string>
        {
        }

        [Fact]
        public void AddToServiceCollection_1()
        {
            var collection = new ServiceCollection();
            TypeFinder.NearType(this.GetType())
                .WhichImplements(typeof(TestInterface<>))
                .AddToServiceCollection(collection,
                                        ServiceLifetime.Transient);

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Transient &&
                                              sd.ServiceType == typeof(TestClass10) &&
                                              sd.ImplementationType == typeof(TestClass10));

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Transient &&
                                              sd.ServiceType == typeof(TestClass11) &&
                                              sd.ImplementationType == typeof(TestClass11));

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Transient &&
                                  sd.ServiceType == typeof(TestClass12) &&
                                  sd.ImplementationType == typeof(TestClass12));
        }

        [Fact]
        public void AddToServiceCollection_2()
        {
            var collection = new ServiceCollection();
            TypeFinder.NearType(this.GetType())
                .WhichImplements(typeof(TestInterface<>))
                .AddToServiceCollection(collection,
                                        ServiceLifetime.Scoped,
                                        typeof(TestInterface<>));

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Scoped &&
                                              sd.ServiceType == typeof(TestInterface<int>) &&
                                              sd.ImplementationType == typeof(TestClass10));

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Scoped &&
                                              sd.ServiceType == typeof(TestInterface<double>) &&
                                              sd.ImplementationType == typeof(TestClass11));

            collection.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Scoped &&
                                  sd.ServiceType == typeof(TestInterface<string>) &&
                                  sd.ImplementationType == typeof(TestClass12));


        }
    }
}

