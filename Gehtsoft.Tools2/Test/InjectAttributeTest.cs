using FluentAssertions;
using Gehtsoft.Tools2.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gehtsoft.Tools2.UnitTest
{
    public class InjectAttributeTest
    {
        public interface IDependency1
        {
        }

        public class Dependency1 : IDependency1
        {
            public Dependency1() { }
        }

        public interface IDependency2
        {
            IDependency1 Dependency1 { get; }
        }

        public class Dependency2 : IDependency2
        {
            public IDependency1 Dependency1 { get; }

            public Dependency2(IDependency1 dependency1)
            {
                Dependency1 = dependency1;
            }
        }

        public interface IDependency3
        {
        }

        public class TestClass
        {
            [Inject]
            private readonly IDependency1 mDependency1;

            public IDependency1 Dependency1 => mDependency1;

            [Inject]
            public IDependency2 Dependency2 { get; init; }

            [Inject]
            public Dependency2 DependencyAsObject2 { get; init; }

            [Inject]
            public IDependency3 Dependency3 { get; init; }
        }

        [Fact]
        public void TestInjection()
        {
            var services = new ServiceCollection();
            var d1 = new Dependency1();
            services.AddSingleton<IDependency1>(d1);
            services.AddTransient<IDependency2, Dependency2>();
            var sp = services.BuildServiceProvider();

            TestClass tc = new TestClass();
            
            tc.Dependency1.Should().BeNull("initially should be null");
            tc.Dependency2.Should().BeNull("initially should be null");
            tc.DependencyAsObject2.Should().BeNull("initially should be null");
            tc.Dependency3.Should().BeNull("initially should be null");

            tc.PopulateMembers(sp);

            tc.Dependency1.Should().BeSameAs(d1, "should get a singletone from sp");
            tc.Dependency2.Should().NotBeNull("should create via sp");
            tc.Dependency2.Dependency1.Should().BeSameAs(d1, "dependency be initialized");

            tc.DependencyAsObject2.Should().NotBeNull("should create via activator");
            tc.DependencyAsObject2.Dependency1.Should().BeSameAs(d1, "dependency be initialized");

            tc.Dependency3.Should().BeNull("interfaces must be not be required");
        }
    }
}
