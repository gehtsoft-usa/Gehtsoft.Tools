using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Gehtsoft.Tools2.Algorithm
{
    public class FibonacciHeap<TKey, TValue> : ICollection<FibonacciHeap<TKey, TValue>.Node> where TKey : IComparable<TKey>
    {
        public class Node : IComparable<Node>
        {
            internal Node Left { get; set; }
            internal Node Right{ get; set; }
            internal Node Parent{ get; set; }
            internal Node Child{ get; set; }
            internal int Degree { get; set; }
            internal bool Mark { get; set; }

            public TKey Key { get; internal set; }
            public TValue Value { get; private set; }

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public int CompareTo(Node other)
            {
                return Key.CompareTo(other.Key);
            }
        }

        class Enumerator : IEnumerator<Node>
        {
            private FibonacciHeap<TKey, TValue> mHeap;
            private Node mCurrent;
            private bool mBeforeBegin;

            internal Enumerator(FibonacciHeap<TKey, TValue> heap)
            {
                mHeap = heap;
                mCurrent = null;
                mBeforeBegin = true;
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                if (mBeforeBegin)
                {
                    mCurrent = mHeap.Minimum;
                    mBeforeBegin = false;
                }
                else
                {
                    mCurrent = mHeap.NextNode(mCurrent, false);
                }

                return mCurrent != null;

            }

            public void Reset()
            {
                mCurrent = null;
                mBeforeBegin = true;
            }

            public Node Current => mCurrent;

            object IEnumerator.Current => Current;
        }


        internal Node NextNode(Node node, bool ignoreChild)
        {
            //the nodes are the number of double linked-lists
            // +-> node1 --> node2 --> node3 --> node 4 --+
            // |     |                                    |
            // +-------------------------------------------
            //       |
            //       v
            //   +-->node1.1 --> node 1.2 --+
            //   |                          |
            //   +--------------------------+
            if (node == null)
                return null;


            //if node has a child - return a child
            if (node.Child != null && !ignoreChild)
                return node.Child;

            if (node.Parent != null)
            {
                //checks whether a right node is the last one
                if (node.Right == node.Parent.Child)
                {
                    //last child of parent
                    return NextNode(node.Parent, true);
                }
            }
            else
            {
                //check whether to right node is the heap minimum
                if (node.Right == Minimum)
                    return null;
            }

            return node.Right;
        }


        private Node mMinRoot;
        int mNumNodes, mNumTrees, mNumMarkedNodes;

        public int Count => mNumNodes;

        public Node Add(TKey key, TValue value)
        {
            Node newNode = new Node(key, value);
            Add(newNode);
            return newNode;
        }

        public void Add(Node newNode)
        {
            if (newNode == null)
                throw new ArgumentNullException(nameof(newNode));

            if (mMinRoot == null)
            {
                mMinRoot = newNode.Left = newNode.Right = newNode;
            }
            else
            {
                newNode.Right = mMinRoot.Right;
                newNode.Left = mMinRoot;

                // Set Pointers to NewNode
                newNode.Left.Right = newNode;
                newNode.Right.Left = newNode;

                // The new node becomes new MinRoot if it is less than current MinRoot
                if (newNode.CompareTo(mMinRoot) < 0)
                    mMinRoot = newNode;
            }

            // We have one more node in the heap, and it is a tree on the root list
            mNumNodes++;
            mNumTrees++;
            newNode.Parent = null;
        }

        public void Union(FibonacciHeap<TKey, TValue> otherHeap)
        {
            Node Min1, Min2, Next1, Next2;

            if (otherHeap == null || otherHeap.mMinRoot == null) return;

            // We join the two circular lists by cutting each list between its
            // min node and the node after the min.  This code just pulls those
            // nodes into temporary variables so we don't get lost as changes
            // are made.

            Min1 = mMinRoot;
            Min2 = otherHeap.mMinRoot;
            Next1 = Min1.Right;
            Next2 = Min2.Right;

            // To join the two circles, we join the minimum nodes to the next
            // nodes on the opposite chains.  Conceptually, it looks like the way
            // two bubbles join to form one larger bubble.  They meet at one point
            // of contact, then expand out to make the bigger circle.

            Min1.Right = Next2;
            Next2.Left = Min1;
            Min2.Right = Next1;
            Next1.Left = Min2;

            // Choose the new minimum for the heap

            if (Min2.CompareTo(Min1) < 0)
                mMinRoot = Min2;

            // Set the amortized analysis statistics and size of the new heap

            mNumNodes += otherHeap.mNumNodes;
            mNumMarkedNodes += otherHeap.mNumMarkedNodes;
            mNumTrees += otherHeap.mNumTrees;
        }

        public Node Minimum => mMinRoot;

        public Node ExtractMin()
        {
            Node Result;
            FibonacciHeap<TKey, TValue> ChildHeap = null;

            // Remove minimum node and set MinRoot to next node
            if ((Result = Minimum) == null)
                return null;

            mMinRoot = Result.Right;
            Result.Right.Left = Result.Left;
            Result.Left.Right = Result.Right;
            Result.Left = Result.Right = null;

            mNumNodes --;
            if (Result.Mark)
            {
                mNumMarkedNodes --;
                Result.Mark = false;
            }
            Result.Degree = 0;

            // Attach child list of Minimum node to the root list of the heap
            // If there is no child list, then do no work

            if (Result.Child == null)
            {
                if (mMinRoot == Result)
                    mMinRoot = null;
            }

            // If MinRoot==Result then there was only one root tree, so the
            // root list is simply the child list of that node (which is
            // null if this is the last node in the list)
            else if (mMinRoot == Result)
                mMinRoot = Result.Child;
            // If MinRoot is different, then the child list is pushed into a
            // new temporary heap, which is then merged by Union() onto the
            // root list of this heap.
            else
            {
                ChildHeap = new FibonacciHeap<TKey, TValue>();
                ChildHeap.mMinRoot = Result.Child;
            }
            // Complete the disassociation of the Result node from the heap
            if (Result.Child != null)
                Result.Child.Parent = null;
            Result.Child = Result.Parent = null;
            // If there was a child list, then we now merge it with the
            //    rest of the root list
            if (ChildHeap != null)
                Union(ChildHeap);
            // Consolidate heap to find new minimum and do reorganize work
            if (mMinRoot != null)
                Consolidate();
            // Return the minimum node, which is now disassociated with the heap
            // It has Left, Right, Parent, Child, Mark and Degree cleared.
             return Result;
        }

        public bool DecreaseKey(Node theNode, bool minimal, TKey newKey)
        {
            Node theParent;

            if (theNode == null)
                return false;

            if (!minimal)
                if (theNode.Key.CompareTo(newKey) < 0)
                    return false;

            theNode.Key = newKey;

            theParent = theNode.Parent;
            if (theParent != null && theNode.CompareTo(theParent) < 0)
            {
                Cut(theNode, theParent);
                CascadingCut(theParent);
            }

            if (theNode.CompareTo(mMinRoot) < 0)
                mMinRoot = theNode;

            return true;
        }

        public bool Remove(Node theNode)
        {
            if (theNode == null)
                throw new ArgumentNullException(nameof(theNode));

            if (DecreaseKey(theNode, true, default(TKey)))
                ExtractMin();

            return true;
        }

        private void Exchange(ref Node a, ref Node b)
        {
            Node c;
            c = a;
            a = b;
            b = c;
        }

        private void Consolidate()
        {
            Node x, y, w;
            Node[] A = new Node[1 + 8 * sizeof(int)];    // 1+lg(n)
            int I = 0, Dn = 1 + 8 * sizeof(int);
            int d;

            // Initialize the consolidation detection array
            for (I = 0; I < Dn; I++)
                A[I] = null;

            // We need to loop through all elements on root list.
            // When a collision of degree is found, the two trees
            // are consolidated in favor of the one with the lesser
            // element key value.  We first need to break the circle
            // so that we can have a stopping condition (we can't go
            // around until we reach the tree we started with
            // because all root trees are subject to becoming a
            // child during the consolidation).

            mMinRoot.Left.Right = null;
            mMinRoot.Left = null;
            w = mMinRoot;

             do {
                x = w;
                d = x.Degree;
                w = w.Right;

                // We need another loop here because the consolidated result
                // may collide with another large tree on the root list.

                while (A[d] != null)
                {
                    y = A[d];
                    if (y.CompareTo(x) < 0)
                        Exchange(ref x, ref y);
                    if (w == y)
                        w = y.Right;
                    Link(y, x);
                    A[d] = null;
                    d++;
                }
                A[d] = x;

             } while (w != null);

            // Now we rebuild the root list, find the new minimum,
            // set all root list nodes' parent pointers to null and
            // count the number of subtrees.
            mMinRoot = null;
            mNumTrees = 0;
            for (I = 0; I < Dn; I++)
                if (A[I] != null)
                    AddToRootList(A[I]);
        }

        private void Link(Node y, Node x)
        {
            // Remove node y from root list
            if (y.Right != null)
                y.Right.Left = y.Left;
            if (y.Left != null)
                y.Left.Right = y.Right;
            mNumTrees--;

            // Make node y a singleton circular list with a parent of x

            y.Left = y.Right = y;
            y.Parent = x;

            // If node x has no children, then list y is its new child list
            if (x.Child == null)
                x.Child = y;
            // Otherwise, node y must be added to node x's child list
            else
            {
                y.Left = x.Child;
                y.Right = x.Child.Right;
                x.Child.Right = y;
                y.Right.Left = y;
            }

            // Increase the degree of node x because it's now a bigger tree
            x.Degree ++;

            // Node y has just been made a child, so clear its mark
            if (y.Mark)
                mNumMarkedNodes--;
            y.Mark = false;
        }

        private void AddToRootList(Node x)
        {
            if (x.Mark) mNumMarkedNodes --;
                x.Mark = false;
            mNumNodes--;
            Add(x);
        }

        private void Cut(Node x, Node y)
        {
            if (y.Child == x)
                y.Child = x.Right;
            if (y.Child == x)
                y.Child = null;

            y.Degree --;

            x.Left.Right = x.Right;
            x.Right.Left = x.Left;

            AddToRootList(x);
        }

        private void CascadingCut(Node y)
        {
            Node z = y.Parent;

            while (z != null)
            {
                if (!y.Mark)
                {
                    y.Mark = true;
                    mNumMarkedNodes++;
                    z = null;
                }
                else
                {
                    Cut(y, z);
                    y = z;
                    z = y.Parent;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Clear()
        {
            mMinRoot = null;
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Node node)
        {
            foreach (Node node1 in this)
                if (object.ReferenceEquals(node, node1))
                    return true;
            return false;
        }

        public void CopyTo(Node[] array, int offset)
        {
            foreach (Node node1 in this)
                array[offset++] = node1;
        }
    }
}
