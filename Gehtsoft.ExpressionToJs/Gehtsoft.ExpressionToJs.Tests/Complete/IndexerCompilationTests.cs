using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>Compilation-level checks for indexer (get_Item) access.</summary>
    public class IndexerCompilationTests : DifferentialTestBase
    {
        public class Holder
        {
            public List<int> Numbers { get; set; }
        }

        // ---------- §1: get_Item on a non-parameter receiver (expected RED) ----------
        // When the indexed object is not a bare parameter (here it is a property), AddCall takes
        // the branch at ExpressionWalker.cs:562 which reads Arguments[1] of a single-argument
        // indexer -> IndexOutOfRangeException while compiling.
        [Fact]
        public void IndexerOnPropertyList_CompilesToJsvIndex()
        {
            Expression<Func<Holder, int>> expr = h => h.Numbers[1];
            string js;
            try { js = Compile(expr); }
            catch (Exception ex) { Assert.Fail($"compilation threw {ex.GetType().Name}: {ex.Message}"); return; }
            Assert.Contains("jsv_index", js);
        }
    }
}
