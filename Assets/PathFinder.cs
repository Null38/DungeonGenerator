using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;


namespace Astar
{

    public static class PathFinder
    {
        public delegate int CostHandler(Vector2Int position);
        public delegate bool PassableHandler(Vector2Int position);

        [Serializable]
        private class Node
        {
            const int StraightCost = 10;
            const int DiagonalCost = 14;

            public Vector2Int Position;
            public Node Parent;
            public int G;
            public int H;
            public int F => G + H;

            public Node(Vector2Int pos, Node parent, Vector2Int target, int costMult = 1)
            {
                Position = pos;
                Parent = parent;
                H = Mathf.Abs(target.x - pos.x) + Mathf.Abs(target.y - pos.y);
                if (parent != null)
                {
                    int dx = Mathf.Abs(parent.Position.x - pos.x);
                    int dy = Mathf.Abs(parent.Position.y - pos.y);
                    G = parent.G + (dx == 1 && dy == 1 ? DiagonalCost : StraightCost) * costMult;
                }
                else
                    G = 0;
            }

            public override bool Equals(object obj)
            {
                return obj is Node node && Position == node.Position;
            }

            public override int GetHashCode()
            {
                return Position.GetHashCode();
            }
        }

        public static readonly Vector2Int[] s_FourDirs = new Vector2Int[]
        {
            new Vector2Int(0 , 1 ), 
            new Vector2Int(1 , 0 ),  
            new Vector2Int(0 , -1), 
            new Vector2Int(-1, 0 ) 
        };

        public static readonly Vector2Int[] s_DiagDirs = new Vector2Int[]
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, bool useDiagonal = true)
        {
            return FindPath(start, target, GetCost, IsPassable, useDiagonal);
        }

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, CostHandler GetCost, PassableHandler IsPassable, bool useDiagonal = true)
        {
            PriorityQueue<Node, int> openSet = new PriorityQueue<Node, int> ();
            HashSet<Node> closedSet = new HashSet<Node>();

            openSet.Enqueue(new Node(start, null, target), 0);

            while (openSet.Count > 0)
            {
                Node currNode = openSet.Dequeue();

                if (closedSet.Contains(currNode))
                    continue;
                
                if (currNode.Position == target)
                    return GetPath(currNode);


                closedSet.Add(currNode);


                foreach (Vector2Int dir in s_FourDirs)
                {
                    Vector2Int newPos = currNode.Position + dir;

                    if (!IsPassable(newPos))
                        continue;

                    Node newNode = new Node(newPos, currNode, target, GetCost(newPos));

                    if (closedSet.Contains(newNode))
                        continue;

                    openSet.Enqueue(newNode, newNode.F);
                }

                if (useDiagonal)
                {
                    foreach (Vector2Int dir in s_DiagDirs)
                    {
                        Vector2Int newPos = currNode.Position + dir;

                        if (!IsPassable(newPos))
                            continue;

                        Node newNode = new Node(newPos, currNode, target, GetCost(newPos));

                        if (closedSet.Contains(newNode))
                            continue;

                        openSet.Enqueue(newNode, newNode.F);
                    }
                }
                
            }

            return null;
        }

        public static int GetCost(Vector2Int position) {return 1;}
        public static bool IsPassable(Vector2Int position)
        {
            Collider2D hit = Physics2D.OverlapPoint(position, LayerMask.GetMask("Wall"));
            
            if (hit == null)
                return true;

            return false;
        }

        private static List<Vector2Int> GetPath(Node curr)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            while (curr != null)
            {
                path.Add(curr.Position);
                curr = curr.Parent;
            }

            path.Reverse(); 
            return path;
        }
    }
}