using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct SupportData
{
    public float3 pos;
    public ColliderComponent collider;
}

public struct ContactData
{
    public float3 worldSpacePointA;
    public float3 worldSpacePointB;
    public float3 normal;
    public float3 tangentA;
    public float3 tangentB;
    public float depth;
}

public static class SupportHelper
{
    [BurstCompile]
    public static void Support(ref float3 search, ref SupportData aData, ref SupportData bData, ref int uniqueId, out SupportPoint assignment)
    {
        float3 tempA = float3.zero, tempB = float3.zero;
        float3 negativeSearch = -search;
        switch (aData.collider.type)
        {
            case ColliderType.Box:
                boxSupport(ref negativeSearch, ref aData, out tempA);
                break;
            case ColliderType.Sphere:
                sphereSupport(ref negativeSearch, ref aData, out tempA);
                break;
            case ColliderType.Capsule:
                capsuleSupport(ref negativeSearch, ref aData, out tempA);
                break;
            case ColliderType.Plane:
                //boxSupport(ref search, ref aData, ref tempA);
                break;
        }

        switch (bData.collider.type)
        {
            case ColliderType.Box:
                boxSupport(ref search, ref bData, out tempB);
                break;
            case ColliderType.Sphere:
                sphereSupport(ref search, ref bData, out tempB);
                break;
            case ColliderType.Capsule:
                capsuleSupport(ref search, ref bData, out tempB);
                break;
            case ColliderType.Plane:
                //SupportHelper.boxSupport(ref negativeSearch, ref bData, ref tempB);
                break;
        }
        assignment = new SupportPoint(uniqueId++);
        assignment.aSupport = tempA;
        assignment.bSupport = tempB;
        assignment.v = tempB - tempA;
    }

    [BurstCompile]
    public static bool validSupport(ref SupportData aData, ref SupportData bData)
    {
        return aData.collider.type != ColliderType.Other && bData.collider.type != ColliderType.Other;
    }

    [BurstCompile]
    public static void boxSupport(ref float3 search, ref SupportData data, out float3 result)
    {
        float4x4 mat = data.collider.localToWorld;

        float3 size = data.collider.halfSize;
        NativeArray<float3> vertices = new NativeArray<float3>(8, Allocator.Temp);
        vertices[0] = new float3(-size.x, -size.y, -size.z);
        vertices[1] = new float3( size.x, -size.y, -size.z);
        vertices[2] = new float3( size.x,  size.y, -size.z);
        vertices[3] = new float3(-size.x,  size.y, -size.z);
        vertices[4] = new float3(-size.x, -size.y,  size.z);
        vertices[5] = new float3( size.x, -size.y,  size.z);
        vertices[6] = new float3( size.x,  size.y,  size.z);
        vertices[7] = new float3(-size.x,  size.y,  size.z);

        float3 d;
        float maxDot = float.NegativeInfinity;
        result = float3.zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            d = math.transform(mat, vertices[i]);
            float dot = math.dot(d, search);
            if (dot > maxDot)
            {
                maxDot = dot;
                result = d;
            }
        }
        vertices.Dispose();
    }

    [BurstCompile]
    public static void sphereSupport(ref float3 search, ref SupportData data, out float3 result)
    {
        float3 pos = data.pos;
        float radius = data.collider.radius;
        float3 d = math.normalize(search);
        result = pos + radius * math.normalize(d);
    }

    [BurstCompile]
    public static void capsuleSupport(ref float3 search, ref SupportData data, out float3 result)
    {
        float4x4 mat = data.collider.localToWorld;
        var inverse = data.collider.localToWorldInverse;
        search = math.transform(inverse, search);

        float3 searchxz = new float3(search.x, 0, search.z);
        float3 searchResult = math.normalize(searchxz) * data.collider.radius;
        searchResult.y = (search.y > 0) ? data.collider.yCap : data.collider.yBase;

        result = math.transform(mat, searchResult) + data.pos;
    }
}

public struct SupportPoint
{
    public float3 v;
    public float3 aSupport;
    public float3 bSupport;
    public int uniqueId;

    public SupportPoint(int uniqueId = 0)
    {
        this.uniqueId = uniqueId;
        v = new float3();
        aSupport = new float3();
        bSupport = new float3();
    }
};

public struct Simplex
{
    public int n;
    public SupportPoint a, b, c, d;
}


public struct Triangle
{
    public SupportPoint a, b, c;
    public float3 n;

    public Triangle(in SupportPoint a, in SupportPoint b, in SupportPoint c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        n = math.normalize(math.cross((b.v - a.v), (c.v - a.v)));
    }
}

public struct Edge
{
    public SupportPoint aPoint;
    public SupportPoint bPoint;
}

public static class EPA
{
    const int MAX_NUM_ITER = 64;
    const float GROWTH_TOLERANCE = 0.0001f;

    public static void AddEdge(ref NativeList<Edge> edges, ref SupportPoint a, ref SupportPoint b)
    {
        for (int i = 0; i < edges.Length; i++)
        {
            if (edges[i].aPoint.uniqueId == b.uniqueId && edges[i].bPoint.uniqueId == a.uniqueId)
            {
                edges.RemoveAtSwapBack(i);
                return;
            }
        }
        edges.Add(new Edge() { aPoint = a, bPoint = b });
    }

    public static bool isValid(float value)
    {
        return !float.IsInfinity(value) && !float.IsNaN(value);
    }

    public static bool epa(ref Simplex simplex, ref SupportData aSupport, ref SupportData bSupport, ref int uniqueId, out ContactData data)
    {
        NativeList<Triangle> triangles = new NativeList<Triangle>(Allocator.Temp)
        {
            new Triangle(simplex.a, simplex.b, simplex.c),
            new Triangle(simplex.a, simplex.c, simplex.d),
            new Triangle(simplex.a, simplex.d, simplex.b),
            new Triangle(simplex.b, simplex.d, simplex.c)
        };

        int closestTriangleIndex = 0;
        bool converged = false;
        for (int iter = 0; iter < MAX_NUM_ITER; iter++)
        {
            NativeList<Edge> edges = new NativeList<Edge>(Allocator.Temp);
            float minDist = float.MaxValue;
            for(int i = 0; i < triangles.Length; i++) 
            {
                float dist = math.dot(triangles[i].n, triangles[i].a.v);
                if(dist < minDist)
                {
                    closestTriangleIndex = i;
                    minDist = dist;
                }
            }
            float3 closestTriangleNormal = triangles[closestTriangleIndex].n;
            SupportHelper.Support(ref closestTriangleNormal, ref aSupport, ref bSupport, ref uniqueId, out SupportPoint search);

            float newDist = math.dot(triangles[closestTriangleIndex].n, search.v);
            float growth = newDist - minDist;
            if(growth < GROWTH_TOLERANCE)
            {
                converged = true;
                break;
            }
            for (int i = 0; i < triangles.Length; i++)
            {
                //can this face be seen from the new search direction? 
                if(math.dot(triangles[i].n, search.v - triangles[i].a.v)> 0)
                {
                    SupportPoint a = triangles[i].a;
                    SupportPoint b = triangles[i].b;
                    SupportPoint c = triangles[i].c;

                    AddEdge(ref edges, ref a, ref b);
                    AddEdge(ref edges, ref b, ref c);
                    AddEdge(ref edges, ref c, ref a);

                    triangles.RemoveAtSwapBack(i);
                    i--;
                }
            }


            // create new triangles from the edges in the edge list
            for(int i = 0; i < edges.Length; i++)
            {
                SupportPoint a = edges[i].aPoint;
                SupportPoint b = edges[i].bPoint;
                triangles.Add(new Triangle(search, a, b));
            }
            edges.Dispose();
        }

        data = new ContactData();
        Triangle closestTriangle = triangles[closestTriangleIndex];
        triangles.Dispose();
   
        float distanceFromOrigin = math.dot(closestTriangle.n, closestTriangle.a.v);

        float u = 0, v =0 , w = 0;

        barycentric(closestTriangle.n * distanceFromOrigin, closestTriangle.a.v, closestTriangle.b.v, closestTriangle.c.v, ref u, ref v, ref w);

        if(!isValid(u) || !isValid(v) || !isValid(w))
        {
            return false;
        }

        if(math.abs(u) > 1 || math.abs(v) > 1 || math.abs(w) > 1)
        {
            return false;
        }

        float3 aPoint = new float3(closestTriangle.a.aSupport * u + closestTriangle.b.aSupport * v + closestTriangle.c.aSupport * w);
        float3 bPoint = new float3(closestTriangle.a.bSupport * u + closestTriangle.b.bSupport * v + closestTriangle.c.bSupport * w);

        //position of a + normal * depth;
        //position of b + -normal * depth;

        data.normal = math.normalize(closestTriangle.n);
        data.depth = distanceFromOrigin;
        data.worldSpacePointA = aPoint;
        data.worldSpacePointB = bPoint;

        return converged;
    }

    public static void barycentric(in float3 p, in float3 a, in float3 b, in float3 c, ref float u, ref float v, ref float w)
    {
        // code from Crister Erickson's Real-Time Collision Detection
        float3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = math.dot(v0, v0);
        float d01 = math.dot(v0, v1);
        float d11 = math.dot(v1, v1);
        float d20 = math.dot(v2, v0);
        float d21 = math.dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        v = (d11 * d20 - d01 * d21) / denom;
        w = (d00 * d21 - d01 * d20) / denom;
        u = 1.0f - v - w;
    }
}



public static class GJK
{
 
    public static bool intersect(ref SupportData aData, ref SupportData bData, ref ContactData contactData, int MAX_ITERATIONS = 64)
    {
        int uniqueId = 0;

        if (!SupportHelper.validSupport(ref aData, ref bData))
        {
            return false;
        }

        float3 search = aData.pos - bData.pos;

        SupportPoint d = new SupportPoint();

        SupportHelper.Support(ref search, ref aData, ref bData, ref uniqueId, out SupportPoint c);

        search = -c.v;

        SupportHelper.Support(ref search, ref aData, ref bData, ref uniqueId, out SupportPoint b);

        if (math.dot(b.v, search) < 0)
        {
            return false;
        }

        search = math.cross(math.cross(c.v - b.v, -b.v), c.v - b.v);
        if (search.x == 0 && search.y == 0 && search.z == 0)
        {
            search = math.cross(c.v - b.v, new float3(1, 0, 0));
            if (search.Equals(float3.zero))
            {
                search = math.cross(c.v - b.v, new float3(0, 0, -1));
            }
        }

        int n = 2;

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            SupportHelper.Support(ref search, ref aData, ref bData, ref uniqueId, out SupportPoint a);
            if (math.dot(a.v, search) < 0)
            {
                return false;
            }
            n++;
            if (n == 3)
            {
                update3(ref a, ref b, ref c, ref d, ref n, ref search);
            }
            else if (update4(ref a, ref b, ref c, ref d, ref n, ref uniqueId, ref search))
            {
                Simplex simplex = new Simplex
                {
                    a = a,
                    b = b,
                    c = c,
                    d = d,
                    n = n
                };

                return EPA.epa(ref simplex, ref aData, ref bData, ref uniqueId, out contactData);
            }
        }
        return false;
    }

    [BurstCompile]
    public static void update3(ref SupportPoint a, ref SupportPoint b, ref SupportPoint c, ref SupportPoint d, ref int n, ref float3 search)
    {
        float3 normal = math.cross(b.v - a.v, c.v - a.v);
        float3 AO = -a.v;

        n = 2;
        if (math.dot(math.cross(b.v - a.v, normal), AO) > 0)
        {
            c = a;
            search = math.cross(math.cross(b.v - a.v, AO), b.v - a.v);
            return;
        }

        if (math.dot(math.cross(normal, c.v - a.v), c.v - a.v) > 0)
        {
            b = a;
            search = math.cross(math.cross(c.v - a.v, AO), c.v - a.v);
            return;
        }
        n = 3;
        if(math.dot(normal, AO) > 0)
        {
            d = c;
            c = b;
            b = a;
            search = normal;
            return;
        }
        d = b;
        b = a;
        search = -normal;
    }

    [BurstCompile]
    public static bool update4(ref SupportPoint a, ref SupportPoint b, ref SupportPoint c, ref SupportPoint d, ref int n, ref int uniqueId, ref float3 search)
    {
        float3 abc = math.cross(b.v - a.v, c.v - a.v);
        float3 acd = math.cross(c.v - a.v, d.v - a.v);
        float3 adb = math.cross(d.v - a.v, b.v - a.v);

        float3 ao = -a.v;
        n = 3;

        if(math.dot(abc, ao) > 0)
        {
            d = c;
            c = b;
            b = a;
            search = abc;
            return false;
        }

        if(math.dot(acd, ao) > 0)
        {
            b = a;
            search = acd;
            return false;
        }

        if(math.dot(adb, ao) > 0)
        {
            c = d;
            d = b;
            b = a;
            search = adb;
            return false;
        }
        return true;
    }





}
