using System;


namespace BinaryTree
{
    /// <summary>
    /// 바이너리 서치 트리 구현용
    /// </summary>
    /// <typeparam name="Key">정렬 기준 타입</typeparam>
    /// <typeparam name="Data">데이터 타입</typeparam>
    public class RedBlackTree<Key, Data>
        where Key : IComparable<Key>
    {
        enum NodeColor { Red, Black };

        Node root = null;

        public delegate Key GetKey_(Data data);
        GetKey_ GetKey;

        public RedBlackTree(GetKey_ getKey)
        {
            GetKey = getKey;
        }

        class Node
        {
            public NodeColor color = NodeColor.Red;

            public Key Key { get; }
            public Data Data { get; }

            Node left_;
            Node right_;

            public Node left
            {
                get => left_;
                set
                {
                    left_ = value;
                    if (value != null)
                        value.parent = this;
                }
            }

            public Node right
            {
                get => right_;
                set
                {
                    right_ = value;
                    if (value != null)
                        value.parent = this;
                }
            }

            public Node parent = null;

            public Node(Key key, Data data)
            {
                Key = key;
                Data = data;
            }
        }


        public void Insert(Data data)
        {
            Key key = GetKey(data);

            if (root == null)
            {
                root = new Node(key, data);
                root.color = NodeColor.Black;
                return;
            }

            Node prev = null;
            Node curr = root;

            while (curr != null)
            {
                prev = curr;
                curr = key.CompareTo(curr.Key) < 0 ? curr.left : curr.right;
            }

            Node newNode = new Node(key, data);

            if (newNode.Key.CompareTo(prev.Key) < 0)
            {
                prev.left = newNode;
            }
            else
            {
                prev.right = newNode;
            }

            InsertFixup(newNode);
        }

        private void InsertFixup(Node node)
        {
            while (node != root && node.parent.color == NodeColor.Red)
            {
                Node grandparent = node.parent.parent;
                Node uncle = node.parent == grandparent.left
                           ? grandparent.right
    : grandparent.left;
                bool side = (node.parent == grandparent.left) ? true : false;

                if (uncle != null && uncle.color == NodeColor.Red) //case 1
                {
                    node.parent.color = NodeColor.Black;
                    if (uncle != null)
                        uncle.color = NodeColor.Black;
                    grandparent.color = NodeColor.Red;

                    node = grandparent;
                }
                else //case 2
                {
                    if (node != (side ? node.parent.left : node.parent.right)) //case 2-1
                    {
                        node = node.parent;
                        if (side)
                            RotateLeft(node);
                        else
                            RotateRight(node);
                    }

                    node.parent.color = NodeColor.Black;
                    grandparent.color = NodeColor.Red;
                    if (side)
                        RotateRight(grandparent);
                    else
                        RotateLeft(grandparent);
                }

            }

            root.color = NodeColor.Black;
        }

        private void RotateRight(Node node)
        {
            Node leftNode = node.left;

            node.left = leftNode.right;

            if (node.parent == null)
            {
                root = leftNode;
            }
            else if (node == node.parent.right)
            {
                node.parent.right = leftNode;
            }
            else
            {
                node.parent.left = leftNode;
            }

            leftNode.right = node;
        }

        private void RotateLeft(Node node)
        {
            Node rightNode = node.right;

            node.right = rightNode.left;


            if (node.parent == null)
            {
                root = rightNode;
            }
            else if (node == node.parent.right)
            {
                node.parent.right = rightNode;
            }
            else
            {
                node.parent.left = rightNode;
            }

            rightNode.left = node;
        }
    }
}