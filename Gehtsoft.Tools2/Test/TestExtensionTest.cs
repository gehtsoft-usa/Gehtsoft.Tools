using FluentAssertions;
using Gehtsoft.Tools2.Algorithm.DFA;
using Gehtsoft.Tools2.Extensions;
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
}

