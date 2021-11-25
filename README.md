# DHI.Mesh

This library contains data structures for easing implementing algorithms on 2D mesh type data, 
i.e. unstructured, triangulated data. It can contain quadrilateral elements as well as triangular elements.

From within DHI, mesh type data is present in the .mesh file and in the .dfsu files.

The following algorithms are currently implemented:
 - Searching for elements containing a point or within an extent/polygon.
 - Extract mesh boundary
 - Interpolation: Calculate values in space, interpolating based on mesh element center values.
 - Mesh-polygon intersection and element intersection weight calculations
 - Find connected sub-meshes.

## Mesh Data classes
`DHI.Mesh` contains two basic data structures representing a mesh, `MeshData` and `SMeshData`. 
They represent the same mesh in two different ways.

The `MeshData` class represents the mesh in an object oriented manner, referencing data as objects, 
and representing the mesh as a set of `MeshNode` and `MeshElement` objects. 

The `SMeshData` class represents the mesh using low-level structures as arrays 
to save on memory and improve performance and referencing data through indices. 
For big meshes, the `SMeshData` can often half the memory usage, and some algorithms are several times
faster on `SMeshData` than the similar algorithm on the `MeshData` structure.

The recommendation is to use `MeshData` for building up new meshes and manipulate with the mesh geometry, 
and use the `SMeshData` for mesh processing and calculations. 
Not all algorithms are available for both data structures. 

There are routines for easy conversion between `MeshData` and `SMeshData`, 
as e.g. `meshData.ToSMesh()` and `smeshData.ToMesh()`.

## Algorithms

### Extract mesh boundary
For a given mesh, the boundary is extracted. A line for each boundary-code is returned.

### Interpolation
For mesh values defined at element center, DHI.Mesh can calculate an interpolated value anywhere within the mesh. The approach is as follows:
 - Node values are calculated based on a Pseudo-Laplacian interpolation procedure.
 - for a given point, interpolation using node and element center values are applied, such that:
   - If a value in the center of an element is requested, the element center value is returned.
   - On the straight line between two element center values, linear interpolation between
     these two element center values are returned. 
   - Left or right of this straight line between two element center values, the nearest node
     value is also taken into account in the interpolation, doing triangular interpolation.

This approach can be used for interpolation of data from one mesh to another. It will not apply any smoothing
when interpolating to identical elements.
